import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions, EventInput, EventChangeArg, EventClickArg, DateSelectArg } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import { CalendarService } from '../../services/calendar.service';
import { CalendarActivity } from '../../models/calendar-activity.model';
import { AktivnostService } from '../../services/aktivnost.service';
import { Aktivnost } from '../../models/aktivnost.model';
import { AktivnostModalComponent } from '../aktivnost-modal/aktivnost-modal.component';
import { BauBatchModalComponent } from '../bau-batch-modal/bau-batch-modal.component';
import { KlijentService } from '../../services/klijent.service';
import { Klijent } from '../../models/klijent.model';
import { CodebookEntry, CodebookService } from '../../services/codebook.service';

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
  imports: [CommonModule, RouterModule, FullCalendarModule, AktivnostModalComponent, BauBatchModalComponent],
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.css']
})
export class CalendarComponent implements OnInit {
  calendarOptions = signal<CalendarOptions>({
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
    initialView: 'dayGridMonth',
    customButtons: {
      bauBatch: {
        text: 'BAU unos',
        click: this.openBauBatchModal.bind(this)
      }
    },
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,timeGridDay bauBatch'
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
    select: this.handleDateSelect.bind(this),
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
  showBauBatchModal = false;
  isDayView = false;
  selectedDay = new Date();
  klijenti: Klijent[] = [];
  bauTypes: CodebookEntry[] = [];
  currentAktivnost: Aktivnost = {
    id: 0,
    opis: '',
    detalji: '',
    datum: new Date(),
    status: 'Planirana',
    vrsta: 'Razvoj',
    bau: false,
    projekatId: 0
  };

  constructor(
    private calendarService: CalendarService,
    private aktivnostService: AktivnostService,
    private klijentService: KlijentService,
    private codebookService: CodebookService
  ) {}

  ngOnInit(): void {
    this.loadBauBatchLookups();
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
          backgroundColor: this.getActivityColor(activity.bau),
          borderColor: this.getActivityColor(activity.bau),
          extendedProps: {
            projectName: activity.projectName,
            projectId: activity.projectId,
            type: activity.type,
            bau: activity.bau
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
    this.isDayView = dateInfo.view.type === 'timeGridDay';
    this.selectedDay = dateInfo.start;
    setTimeout(() => this.updateBauBatchToolbarButton(), 0);
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

  handleDateSelect(selectInfo: DateSelectArg): void {
    // Open modal to create new activity with selected date/time
    this.currentAktivnost = {
      id: 0,
      opis: '',
      detalji: '',
      datum: selectInfo.start,
      startUtc: selectInfo.start.toISOString(),
      endUtc: selectInfo.end.toISOString(),
      status: 'Planirana',
      vrsta: 'Razvoj',
      bau: false, // Default to non-BAU
      projekatId: 0 // Will be selected in modal
    };
    this.showAktivnostModal = true;

    // Clear the selection
    const calendarApi = selectInfo.view.calendar;
    calendarApi.unselect();
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
    } else {
      // Create new
      this.aktivnostService.create(aktivnost).subscribe({
        next: () => {
          this.closeAktivnostModal();
          // Refresh calendar events
          window.location.reload();
        },
        error: (error) => {
          console.error('Error creating activity:', error);
          this.error = 'Greška pri kreiranju aktivnosti';
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
      bau: false,
      projekatId: 0
    };
  }

  refreshCalendar(): void {
    // Refresh calendar events
    window.location.reload();
  }

  openBauBatchModal(): void {
    if (!this.isDayView) {
      return;
    }

    this.showBauBatchModal = true;
  }

  closeBauBatchModal(): void {
    this.showBauBatchModal = false;
  }

  onBauBatchSaved(): void {
    this.showBauBatchModal = false;
    this.refreshCalendar();
  }

  onAktivnostDeleted(id: number): void {
    // Activity was deleted from modal, refresh the calendar
    window.location.reload();
  }

  getActivityColor(isBau: boolean): string {
    // BAU activities: olive-green (darker for better text contrast)
    // Project activities: darker blue (darker than header)
    return isBau ? '#7cb342' : '#2980b9';
  }

  private loadBauBatchLookups(): void {
    this.klijentService.getAll().subscribe({
      next: (data) => {
        this.klijenti = data;
      },
      error: (err) => {
        console.error('Error loading clients:', err);
      }
    });

    this.codebookService.getByEntityName('BauActivityType').subscribe({
      next: (data) => {
        this.bauTypes = data;
      },
      error: (err) => {
        console.error('Error loading BAU activity types:', err);
      }
    });
  }

  private updateBauBatchToolbarButton(): void {
    const button = document.querySelector('.fc-bauBatch-button') as HTMLButtonElement | null;
    if (!button) return;

    button.style.display = this.isDayView ? '' : 'none';
    button.title = 'Zbirni unos BAU aktivnosti';
  }
}
