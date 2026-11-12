#!/usr/bin/env node
/**
 * Frontend servis dosyalarındaki tüm `apiService.<get|post|put|patch|delete><T>(...)` çağrılarını çıkarır ve
 * T tipini (zorunlu/opsiyonel alanlar, tip türleri) JSON şemasına çevirir.
 * Çıktı: scripts/smoke-api/endpoints.json  (run.mjs tarafından okunur)
 * Gereksinim: frontend/aynesil-web/node_modules (typescript) kurulu olmalı.
 */
import { createRequire } from 'node:module'
import path from 'node:path'
import fs from 'node:fs'
import { fileURLToPath } from 'node:url'

const here = path.dirname(fileURLToPath(import.meta.url))
const web = path.resolve(here, '../../frontend/aynesil-web')
const ts = createRequire(path.join(web, 'package.json'))('typescript')

const cfgPath = path.join(web, 'tsconfig.app.json')
const cfg = ts.getParsedCommandLineOfConfigFile(cfgPath, {}, { ...ts.sys, onUnRecoverableConfigFileDiagnostic: () => {} })
const srcDir = path.join(web, 'src')
const walk = (d) => fs.readdirSync(d, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(d, e.name)) : [path.join(d, e.name)])
// services/, stores/, composables/ ... — içinde apiService.<verb> geçen tüm .ts dosyaları
const files = walk(srcDir).filter(f => f.endsWith('.ts') && !f.endsWith('api.service.ts') && /apiService\s*\.(get|post|put|patch|delete)\b/.test(fs.readFileSync(f, 'utf8')))
const program = ts.createProgram({ rootNames: files, options: { ...cfg.options, noEmit: true, skipLibCheck: true } })
const checker = program.getTypeChecker()

const MAX_DEPTH = 4
function schemaOf(type, node, depth = 0) {
  let nullable = false
  if (type.isUnion()) {
    const parts = type.types.filter(t => {
      if (t.flags & (ts.TypeFlags.Null)) { nullable = true; return false }
      if (t.flags & ts.TypeFlags.Undefined) return false
      return true
    })
    if (parts.length === 1) { const s = schemaOf(parts[0], node, depth); s.nullable = s.nullable || nullable; return s }
    if (parts.every(p => p.flags & (ts.TypeFlags.StringLike))) return { kind: 'string', nullable }
    if (parts.every(p => p.flags & (ts.TypeFlags.NumberLike))) return { kind: 'number', nullable }
    if (parts.every(p => p.flags & (ts.TypeFlags.BooleanLike))) return { kind: 'boolean', nullable }
    return { kind: 'any', nullable }
  }
  if (type.flags & ts.TypeFlags.StringLike) return { kind: 'string', nullable }
  if (type.flags & ts.TypeFlags.NumberLike) return { kind: 'number', nullable }
  if (type.flags & ts.TypeFlags.BooleanLike) return { kind: 'boolean', nullable }
  if (type.flags & (ts.TypeFlags.Any | ts.TypeFlags.Unknown)) return { kind: 'any', nullable }
  if (checker.isArrayType(type)) {
    const item = checker.getTypeArguments(type)[0]
    return { kind: 'array', nullable, item: depth >= MAX_DEPTH ? { kind: 'any' } : schemaOf(item, node, depth + 1) }
  }
  if (type.flags & ts.TypeFlags.Object) {
    if (checker.getIndexInfosOfType(type).length && !checker.getPropertiesOfType(type).length) return { kind: 'map', nullable }
    if (depth >= MAX_DEPTH) return { kind: 'object', nullable, props: {} }
    const props = {}
    for (const p of checker.getPropertiesOfType(type)) {
      const pt = checker.getTypeOfSymbolAtLocation(p, node)
      const s = schemaOf(pt, node, depth + 1)
      s.optional = !!(p.flags & ts.SymbolFlags.Optional)
      props[p.name] = s
    }
    return { kind: 'object', nullable, props }
  }
  return { kind: 'any', nullable }
}

const VERBS = ['get', 'post', 'put', 'patch', 'delete']
const out = []
for (const sf of program.getSourceFiles().filter(s => files.includes(s.fileName))) {
  const service = path.basename(sf.fileName).replace(/\.(service|store)\.ts$/, m => m)
  ts.forEachChild(sf, function visit(node) {
    if (ts.isCallExpression(node) && ts.isPropertyAccessExpression(node.expression)
        && VERBS.includes(node.expression.name.text) && node.expression.expression.getText() === 'apiService') {
      const verb = node.expression.name.text
      const urlArg = node.arguments[0]
      if (!urlArg) return
      // path + query ayrıştır
      let pathParts = [], pathParams = [], hasQs = false
      const addExpr = (e) => {
        const t = checker.getTypeAtLocation(e)
        if (t.isStringLiteral()) { pathParts.push(t.value); return }
        const txt = e.getText()
        if (/^qs\(/.test(txt)) { hasQs = true; pathParts.push('{{QS}}'); return }
        pathParts.push(`{{${txt}}}`); pathParams.push(txt)
      }
      if (ts.isNoSubstitutionTemplateLiteral(urlArg) || ts.isStringLiteral(urlArg)) pathParts.push(urlArg.text)
      else if (ts.isTemplateExpression(urlArg)) {
        pathParts.push(urlArg.head.text)
        for (const sp of urlArg.templateSpans) { addExpr(sp.expression); pathParts.push(sp.literal.text) }
      } else addExpr(urlArg)
      const typeArg = node.typeArguments?.[0]
      const schema = typeArg ? schemaOf(checker.getTypeFromTypeNode(typeArg), typeArg) : { kind: 'any' }
      // metot adı + parametreleri
      let fn = node.parent, method = '?'
      while (fn && !(ts.isArrowFunction(fn) || ts.isFunctionExpression(fn) || ts.isMethodDeclaration(fn) || ts.isFunctionDeclaration(fn))) fn = fn.parent
      if (fn) {
        const holder = fn.parent
        method = (holder && (ts.isPropertyAssignment(holder) || ts.isMethodDeclaration(holder))) ? holder.name.getText() : (fn.name?.getText?.() ?? '?')
      }
      const fnParams = fn ? fn.parameters.map(p => ({ name: p.name.getText(), type: p.type?.getText() ?? '', optional: !!p.questionToken || !!p.initializer })) : []
      const line = sf.getLineAndCharacterOfPosition(node.getStart()).line + 1
      out.push({
        verb,
        id: `${service.replace(/\.(service|store)\.ts$/, '').replace(/\.ts$/, '')}.${method}`,
        file: `${path.relative(path.resolve(here, '../..'), sf.fileName)}:${line}`,
        path: pathParts.join(''), pathParams, hasQs,
        fnParams, typeText: typeArg?.getText() ?? '', schema,
        hasConfigParams: node.arguments.length > 1,
      })
    }
    ts.forEachChild(node, visit)
  })
}
fs.writeFileSync(path.join(here, 'endpoints.json'), JSON.stringify(out, null, 2))
console.log(`${out.length} API çağrısı çıkarıldı (get/post/put/patch/delete) → scripts/smoke-api/endpoints.json`)
