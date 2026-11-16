/** Kurum bazında açılıp kapanan modül kodları — kaynak: src/Aynesil.Shared/Constants/Modules.cs */
export const Modules = {
  Camps: 'camps',
  Consultancy: 'consultancy',
  Camera: 'camera',
  Surveys: 'surveys',
  CareTeam: 'care_team',
  Kpi: 'kpi',
} as const
export type ModuleCode = (typeof Modules)[keyof typeof Modules]
