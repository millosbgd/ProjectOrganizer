import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxChartsModule, Color, ScaleType } from '@swimlane/ngx-charts';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardStats } from '../../models/dashboard.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, NgxChartsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  stats: DashboardStats | null = null;
  loading = true;
  error = false;

  // Chart data
  projectsByStatusData: any[] = [];
  activitiesByMonthData: any[] = [];
  activitiesByStatusData: any[] = [];
  newProjectsByMonthData: any[] = [];

  // Chart options
  view: [number, number] = [700, 300];
  showXAxis = true;
  showYAxis = true;
  gradient = true;
  showLegend = true;
  showXAxisLabel = true;
  showYAxisLabel = true;
  xAxisLabel = 'Mesec';
  yAxisLabel = 'Broj';
  animations = true;

  // Color scheme
  colorScheme: Color = {
    name: 'dashboard',
    selectable: true,
    group: ScaleType.Ordinal,
    domain: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899']
  };

  constructor(private dashboardService: DashboardService) { }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    this.error = false;

    this.dashboardService.getStats().subscribe({
      next: (data) => {
        this.stats = data;
        this.prepareChartData();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading dashboard data:', err);
        this.error = true;
        this.loading = false;
      }
    });
  }

  prepareChartData(): void {
    if (!this.stats) return;

    // Pie Chart - Projekti po statusu
    this.projectsByStatusData = this.stats.projectsByStatus.map(item => ({
      name: item.status,
      value: item.count
    }));

    // Bar Chart - Aktivnosti po mesecima
    this.activitiesByMonthData = [{
      name: 'Aktivnosti',
      series: this.stats.activitiesByMonth.map(item => ({
        name: item.month,
        value: item.count
      }))
    }];

    // Bar Chart - Aktivnosti po statusu
    this.activitiesByStatusData = this.stats.activitiesByStatus.map(item => ({
      name: item.status,
      value: item.count
    }));

    // Line Chart - Novi projekti po mesecima
    this.newProjectsByMonthData = [{
      name: 'Novi Projekti',
      series: this.stats.newProjectsByMonth.map(item => ({
        name: item.month,
        value: item.count
      }))
    }];
  }

  onSelect(event: any): void {
    console.log('Chart item selected:', event);
  }

  onRefresh(): void {
    this.loadDashboardData();
  }
}
