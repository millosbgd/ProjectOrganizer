import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions, EventInput, EventChangeArg } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import hrLocale from '@fullcalendar/core/locales/hr';
import { CalendarService } from '../../services/calendar.service';
import { CalendarActivity } from '../../models/calendar-activity.model';

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule, FullCalendarModule],
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.css']
})
export class CalendarComponent implements OnInit {
  calendarOptions = signal<CalendarOptions>({
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
    initialView: 'dayGridMonth',
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,timeGridDay'
    },
    editable: true,
    selectable: true,
    selectMirror: true,
    dayMaxEvents: true,
    weekends: true,
    events: this.loadEvents.bind(this),
    eventChange: this.handleEventChange.bind(this),
    eventDrop: this.handleEventChange.bind(this),
    eventResize: this.handleEventChange.bind(this),
    datesSet: this.handleDatesSet.bind(this),
    eventTimeFormat: {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    },
    slotLabelFormat: {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    },
    firstDay: 1, // Monday
    locale: hrLocale
  });

  loading = false;
  error: string | null = null;

  constructor(private calendarService: CalendarService) {}

  ngOnInit(): void {
    // Initial load will be triggered by datesSet
  }

  loadEvents(fetchInfo: any, successCallback: any, failureCallback: any): void {
    const from = fetchInfo.start.toISOString();
    const to = fetchInfo.end.toISOString();

    this.loading = true;
    this.error = null;

    this.calendarService.getActivities(from, to).subscribe({
      next: (activities: CalendarActivity[]) => {
        const events: EventInput[] = activities.map((activity: CalendarActivity) => ({
          id: activity.id.toString(),
          title: activity.title,
          start: activity.start,
          end: activity.end,
          backgroundColor: this.getActivityColor(activity.type),
          borderColor: this.getActivityColor(activity.type),
          extendedProps: {
            projectName: activity.projectName,
            type: activity.type
          }
        }));
        successCallback(events);
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Error loading activities:', err);
        this.error = 'Greška pri učitavanju aktivnosti';
        failureCallback(err);
        this.loading = false;
      }
    });
  }

  handleEventChange(changeInfo: EventChangeArg): void {
    const event = changeInfo.event;
    const activityId = parseInt(event.id);
    
    if (!event.start || !event.end) {
      changeInfo.revert();
      return;
    }

    const updateDto = {
      startUtc: event.start.toISOString(),
      endUtc: event.end.toISOString()
    };

    this.calendarService.updateActivityTime(activityId, updateDto).subscribe({
      next: () => {
        console.log('Activity time updated successfully');
      },
      error: (err: any) => {
        console.error('Error updating activity:', err);
        this.error = err.error?.message || 'Greška pri ažuriranju aktivnosti';
        changeInfo.revert();
        
        // Clear error after 5 seconds
        setTimeout(() => {
          this.error = null;
        }, 5000);
      }
    });
  }

  handleDatesSet(dateInfo: any): void {
    // This is called when the user navigates to a different date range
    // Events will be automatically refetched via loadEvents
  }

  getActivityColor(type: string): string {
    const colorMap: { [key: string]: string } = {
      'Development': '#3498db',
      'Meeting': '#e74c3c',
      'Testing': '#2ecc71',
      'Documentation': '#f39c12',
      'Planning': '#9b59b6',
      'Default': '#95a5a6'
    };
    return colorMap[type] || colorMap['Default'];
  }
}
