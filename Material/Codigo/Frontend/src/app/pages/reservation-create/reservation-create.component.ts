import { Component, OnInit } from '@angular/core';
import { PharmacyService } from '../../services/pharmacy.service';
import { ReservationService } from '../../services/reservation.service';
import { DrugService } from '../../services/drug.service';
import { Pharmacy } from '../../interfaces/pharmacy';
import { Drug } from '../../interfaces/drug';
import { ReservationRequest, ReservationDetailRequest } from '../../interfaces/reservation';
import { CommonService } from '../../services/CommonService';

@Component({
  selector: 'app-reservation-create',
  templateUrl: './reservation-create.component.html',
  styleUrls: ['./reservation-create.component.css']
})
export class ReservationCreateComponent implements OnInit {
  pharmacies: Pharmacy[] = [];
  selectedPharmacyId: number = 0;
  lastSelectedPharmacyId: number = 0;
  availableDrugs: Drug[] = [];
  drugQuantities: { [key: string]: number } = {};

  reservationDetails: { drugCode: string, name: string, quantity: number }[] = [];
  userName: string = "";
  userEmail: string = "";
  successMessage: string = "";
  publicKey: string = "";
  showSuccessModal: boolean = false;
  hasQuantityError: boolean = false;
  errorMessage: string = "";
  showLimitModal: boolean = false;
  limitModalMessage: string = "";

  constructor(
    private pharmacyService: PharmacyService,
    private reservationService: ReservationService,
    private drugService: DrugService,
    private commonService: CommonService
  ) { }

  ngOnInit(): void {
    this.pharmacyService.getPharmacys().subscribe(data => {
      this.pharmacies = data;
    });
  }

  onPharmacyChange(): void {
    if (this.reservationDetails.length > 0) {
      this.commonService.updateToastData("Una reserva solo puede contener medicamentos de una única farmacia", "danger", "Error");
      setTimeout(() => { this.selectedPharmacyId = this.lastSelectedPharmacyId; });
      return;
    }

    if (this.selectedPharmacyId > 0) {
      this.lastSelectedPharmacyId = this.selectedPharmacyId;
      this.drugService.getDrugsFilter(this.selectedPharmacyId.toString(), "").subscribe(data => {
        this.availableDrugs = data;
        this.availableDrugs.forEach(d => this.drugQuantities[d.code] = 1);
      });
    } else {
      this.availableDrugs = [];
    }
  }

  addDrugToReservation(drug: Drug): void {
    const qty = this.drugQuantities[drug.code];
    if (qty <= 0) {
      this.commonService.updateToastData("Cantidad inválida", "warning", "Atención");
      return;
    }

    const existingTotal = this.reservationDetails
      .filter(d => d.drugCode === drug.code)
      .reduce((sum, d) => sum + d.quantity, 0);

    if (existingTotal + qty > 5) {
      this.commonService.updateToastData("No se permiten más de 5 unidades del mismo medicamento", "danger", "Error");
      this.hasQuantityError = true;
      return;
    }

    const totalAllDrugs = this.reservationDetails.reduce((sum, d) => sum + d.quantity, 0);
    if (totalAllDrugs + qty > 15) {
      this.errorMessage = "La reserva no puede superar las 15 unidades totales";
      this.hasQuantityError = true;
      return;
    }

    this.hasQuantityError = false;
    this.errorMessage = "";
    this.reservationDetails.push({
      drugCode: drug.code,
      name: drug.name,
      quantity: qty
    });
  }

  removeDetail(index: number): void {
    this.reservationDetails.splice(index, 1);
    this.hasQuantityError = false;
    this.errorMessage = "";
  }

  createReservation(): void {
    if (!this.userEmail || this.reservationDetails.length === 0) {
      this.commonService.updateToastData("Faltan datos obligatorios", "danger", "Error");
      return;
    }

    const request: ReservationRequest = {
      pharmacyId: Number(this.selectedPharmacyId),
      userEmail: this.userEmail,
      details: this.reservationDetails.map(d => ({
        drugCode: d.drugCode,
        quantity: d.quantity
      } as ReservationDetailRequest))
    };

    this.reservationService.createReservation(request).subscribe(res => {
      if (res) {
        this.publicKey = res.publicKey;
        this.successMessage = "Reserva creada exitosamente. Guarda tu clave pública.";
        this.showSuccessModal = true;
        this.commonService.updateToastData("Reserva creada exitosamente", "success", "Éxito");
        this.resetForm();
      }
      if (this.reservationService.lastErrorMessage?.includes("No puedes tener más de 10 reservas")) {
        this.showLimitModal = true;
        this.limitModalMessage = this.reservationService.lastErrorMessage;
      }
      this.reservationService.lastErrorMessage = '';
    });
  }

  getPublicKeyPreview(): string {
    if (!this.publicKey) {
      return "";
    }
    if (this.publicKey.length <= 36) {
      return this.publicKey;
    }
    return `${this.publicKey.slice(0, 16)}...${this.publicKey.slice(-12)}`;
  }

  copyPublicKey(): void {
    if (!this.publicKey) {
      return;
    }
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(this.publicKey).then(() => {
        this.commonService.updateToastData("Clave copiada", "success", "Éxito");
      });
      return;
    }
    const textarea = document.createElement("textarea");
    textarea.value = this.publicKey;
    textarea.style.position = "fixed";
    textarea.style.opacity = "0";
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand("copy");
    document.body.removeChild(textarea);
    this.commonService.updateToastData("Clave copiada", "success", "Éxito");
  }

  closeSuccessModal(): void {
    this.showSuccessModal = false;
  }

  closeLimitModal(): void {
    this.showLimitModal = false;
  }

  resetForm(): void {
    this.reservationDetails = [];
    this.userName = "";
    this.userEmail = "";
    this.selectedPharmacyId = 0;
    this.availableDrugs = [];
    this.hasQuantityError = false;
    this.errorMessage = "";
  }
}
