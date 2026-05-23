import { Component, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ReservationResponse } from 'src/app/interfaces/reservation';
import { ReservationService } from 'src/app/services/reservation.service';

@Component({
  selector: 'app-validate-reservations',
  templateUrl: './validate-reservations.component.html',
  styleUrls: ['./validate-reservations.component.css'],
})
export class ValidateReservationsComponent implements OnInit {
  pendingReservations: ReservationResponse[] = [];
  selectedReservation: ReservationResponse | null = null;
  recipePdfSrc: SafeResourceUrl | null = null;
  recipeFileNames: string[] = [];
  visible = false;
  modalMessage = '';

  constructor(
    private reservationService: ReservationService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.loadPending();
  }

  loadPending(): void {
    this.reservationService.getAllPending().subscribe((reservations) => {
      this.pendingReservations = reservations ?? [];
    });
  }

  selectReservation(reservation: ReservationResponse): void {
    this.selectedReservation = reservation;
    this.recipePdfSrc = null;
    if (reservation.hasRecipe && reservation.recipeFiles) {
      this.recipeFileNames = reservation.recipeFiles.map((_, i) => `Receta #${i + 1}`);
    } else {
      this.recipeFileNames = [];
    }
  }

  showRecipe(index: number): void {
    if (this.selectedReservation?.recipeFiles?.[index]) {
      const base64 = this.selectedReservation.recipeFiles[index];
      this.recipePdfSrc = this.sanitizer.bypassSecurityTrustResourceUrl(
        'data:application/pdf;base64,' + base64
      );
    }
  }

  confirmReservation(): void {
    if (!this.selectedReservation) return;
    this.reservationService.confirmReservation(this.selectedReservation.code).subscribe({
      next: () => {
        this.modalMessage = 'Reserva confirmada exitosamente';
        this.visible = true;
        this.selectedReservation = null;
        this.recipePdfSrc = null;
        this.recipeFileNames = [];
        this.loadPending();
      },
      error: () => {
        this.modalMessage = 'Error al confirmar la reserva';
        this.visible = true;
      },
    });
  }

  closeModal(): void {
    this.visible = false;
  }
}
