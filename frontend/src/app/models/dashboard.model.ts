export interface DashboardStats {
  activeProjectsCount: number;
  unfinishedActivitiesCount: number;
  projectsByStatus: ProjectStatusCount[];
  activitiesByMonth: MonthlyActivityCount[];
  activitiesByStatus: ActivityStatusCount[];
  newProjectsByMonth: MonthlyProjectCount[];
}

export interface ProjectStatusCount {
  status: string;
  count: number;
}

export interface MonthlyActivityCount {
  month: string;
  year: number;
  monthNum: number;
  count: number;
}

export interface ActivityStatusCount {
  status: string;
  count: number;
}

export interface MonthlyProjectCount {
  month: string;
  year: number;
  monthNum: number;
  count: number;
}
