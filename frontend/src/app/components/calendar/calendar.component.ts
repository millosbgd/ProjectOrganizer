import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions, EventInput, EventChangeArg, EventClickArg } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import { CalendarService } from '../../services/calendar.service';
import { CalendarActivity } from '../../models/calendar-activity.model';
import { AktivnostService } from '../../services/aktivnost.service';
import { Aktivnost } from '../../models/aktivnost.model';
import { AktivnostModalComponent } from '../aktivnost-modal/aktivnost-modal.component';

// Custom Serbian Latin locale configuration
const serbianLatinLocale = {
  code: 'sr-latn',
  week: {
    dow: 1, // Monday is the first day of the week
    doy: 4  // First week of the year contains Jan 4th
  },
  buttonText: {
    prev: 'Prethodni',
    next: 'Sledeći',
    today: 'Danas',
    month: 'Mesec',
    week: 'Nedelja',
    day: 'Dan',
    list: 'Lista'
  },
  weekText: 'Ned',
  allDayText: 'Ceo dan',
  moreLinkText: (n: number) => '+ još ' + n,
  noEventsText: 'Nema aktivnosti za prikaz',
  buttonHints: {
    prev: 'Prethodni $0',
    next: 'Sledeći $0',
    today: 'Današnji $0'
  },
  viewHint: '$0 pregled',
  navLinkHint: 'Idi na $0',
  moreLinkHint: (eventCnt: number) => `Prikaži još ${eventCnt} događaj${eventCnt !== 1 ? 'a' : ''}`,
  closeHint: 'Zatvori',
  timeHint: 'Vreme',
  eventHint: 'Događaj'
};

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule, FullCalendarModule, AktivnostModalComponent],
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
    eventClick: this.handleEventClick.bind(this),
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
    locale: serbianLatinLocale
  });

  loading = false;
  error: string | null = null;
  showAktivnostModal = false;
  currentAktivnost: Aktivnost = {
    id: 0,
    opis: '',
    detalji: '',
    datum: new Date(),
    status: 'Planirana',
    vrsta: 'Razvoj',
    projekatId: 0
  };

  constructor(
    private calendarService: CalendarService,
    private aktivnostService: AktivnostService
  ) {}

  ngOnInit(): void {
    // Initial load will be triggered by datesSet
  }

  loadEvents(fetchInfo: any, successCallback: any, failureCallback: any): void {
    const from = fetchInfo.start.toISOString();
    const to = fetchInfo.end.toISOString();

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
            projectId: activity.projectId,
            type: activity.type
          }
        }));
        successCallback(events);
      },
      error: (err: any) => {
        console.error('Error loading activities:', err);
        this.error = 'Greška pri učitavanju aktivnosti';
        failureCallback(err);
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

  handleEventClick(clickInfo: EventClickArg): void {
    const activityId = parseInt(clickInfo.event.id);
    
    // Load full activity details
    this.aktivnostService.getById(activityId).subscribe({
      next: (activity: Aktivnost) => {
        this.currentAktivnost = { ...activity };
        this.showAktivnostModal = true;
      },
      error: (err: any) => {
        console.error('Error loading activity:', err);
        this.error = 'Greška pri učitavanju aktivnosti';
        setTimeout(() => {
          this.error = null;
        }, 5000);
      }
    });
  }

  saveAktivnost(aktivnost: Aktivnost): void {
    if (aktivnost.id) {
      // Update existing
      this.aktivnostService.update(aktivnost.id, aktivnost).subscribe({
        next: () => {
          this.closeAktivnostModal();
          // Refresh calendar events
          window.location.reload();
        },
        error: (error) => {
          console.error('Error updating activity:', error);
          this.error = 'Greška pri ažuriranju aktivnosti';
          setTimeout(() => {
            this.error = null;
          }, 5000);
        }
      });
    }
  }

  closeAktivnostModal(): void {
    this.showAktivnostModal = false;
    this.currentAktivnost = {
      id: 0,
      opis: '',
      detalji: '',
      datum: new Date(),
      status: 'Planirana',
      vrsta: 'Razvoj',
      projekatId: 0
    };
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
