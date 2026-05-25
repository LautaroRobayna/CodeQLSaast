import { Component } from '@angular/core';
import { ReservationResponse } from '../../interfaces/reservation';
import { ReservationService } from '../../services/reservation.service';

@Component({
  selector: 'app-reservation-manage',
  templateUrl: './reservation-manage.component.html',
  styleUrls: ['./reservation-manage.component.css']
})
export class ReservationManageComponent {
  publicKey: string = '';
  reservation: ReservationResponse | null = null;
  errorMessage: string = '';
  prescriptionUploaded: boolean = false;
  prescriptionBase64: string = '';
  prescriptionFileName: string = '';

  get showPrescriptionUpload(): boolean {
    return !!this.reservation &&
      (this.reservation.status === 'Pendiente' || this.reservation.status === 'Pending') &&
      !this.prescriptionUploaded &&
      (this.reservation.details?.some(d => d.requiresPrescription) ?? false);
  }

  constructor(private reservationService: ReservationService) {}

  searchReservation(): void {
    if (!this.publicKey.trim()) {
      this.errorMessage = 'Por favor ingresá una clave pública.';
      return;
    }
    this.reservation = null;
    this.errorMessage = '';
    this.prescriptionUploaded = false;
    this.reservationService.getByPublicKey(this.publicKey).subscribe(res => {
      if (res) {
        this.reservation = res;
        this.prescriptionUploaded = res.prescriptionUploaded ?? false;
      } else {
        this.errorMessage = 'Reserva no encontrada.';
      }
    });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    this.prescriptionFileName = file.name;
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      this.prescriptionBase64 = result.split(',')[1];
    };
    reader.readAsDataURL(file);
  }

  get showCancelButton(): boolean {
    return !!this.reservation
  }

  cancelReservation(): void {
    if (!this.reservation) return;
    this.reservationService.cancelReservation(this.publicKey).subscribe(res => {
      if (res) {
        this.reservation = res;
      }
    });
  }

  uploadPrescription(): void {
    if (!this.prescriptionBase64) return;
    this.reservationService.uploadPrescription(this.publicKey, this.prescriptionBase64, this.prescriptionFileName).subscribe(() => {
      this.prescriptionUploaded = true;
    });
  }
}
