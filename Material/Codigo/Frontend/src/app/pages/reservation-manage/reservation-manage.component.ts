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

  constructor(private reservationService: ReservationService) {}

  searchReservation(): void {
    if (!this.publicKey.trim()) return;
    this.reservationService.getByPublicKey(this.publicKey).subscribe(res => {
      if (res) {
        this.reservation = res;
      }
    });
  }
}
