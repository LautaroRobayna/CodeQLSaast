import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
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
  availableDrugs: Drug[] = [];
  drugQuantities: { [key: string]: number } = {};

  reservationDetails: { drugCode: string, name: string, quantity: number, requiresPrescription: boolean }[] = [];
  userName: string = "";
  userEmail: string = "";
  successMessage: string = "";
  publicKey: string = "";
  showSuccessModal: boolean = false;
  prescriptionBase64: string = "";
  prescriptionFileName: string = "";
  prescriptionUploaded: boolean = false;
  prescriptionError: string = "";

  constructor(
    private pharmacyService: PharmacyService,
    private reservationService: ReservationService,
    private drugService: DrugService,
    private commonService: CommonService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.pharmacyService.getPharmacys().subscribe(data => {
      this.pharmacies = data;
    });
  }

  onPharmacyChange(): void {
    if (this.selectedPharmacyId > 0) {
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
    if (qty > 0) {
      this.reservationDetails.push({
        drugCode: drug.code,
        name: drug.name,
        quantity: qty,
        requiresPrescription: drug.prescription
      });
    } else {
      this.commonService.updateToastData("Cantidad inválida", "warning", "Atención");
    }
  }

  get anyRequiresPrescription(): boolean {
    return this.reservationDetails.some(d => d.requiresPrescription);
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    if (!file.name.toLowerCase().endsWith('.pdf') && file.type !== 'application/pdf') {
      this.commonService.updateToastData("Solo se permiten archivos PDF", "danger", "Error");
      input.value = '';
      return;
    }
    this.prescriptionFileName = file.name;
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      this.prescriptionBase64 = result.split(',')[1];
    };
    reader.readAsDataURL(file);
  }

  removeDetail(index: number): void {
    this.reservationDetails.splice(index, 1);
  }

  createReservation(): void {
    if (!this.userEmail || this.reservationDetails.length === 0) {
      this.commonService.updateToastData("Faltan datos obligatorios", "danger", "Error");
      return;
    }

    if (this.anyRequiresPrescription && !this.prescriptionBase64) {
      setTimeout(() => {
        this.prescriptionError = "Debes subir la receta para los medicamentos que la requieren";
      }, 0);
      return;
    }
    this.prescriptionError = "";

    const request: ReservationRequest = {
      pharmacyId: Number(this.selectedPharmacyId),
      userEmail: this.userEmail,
      details: this.reservationDetails.map(d => ({
        drugCode: d.drugCode,
        quantity: d.quantity
      } as ReservationDetailRequest)),
      prescriptionBase64: this.prescriptionBase64 || undefined,
      prescriptionFileName: this.prescriptionFileName || undefined
    };

    this.reservationService.createReservation(request).subscribe(res => {
      if (res) {
        this.publicKey = res.publicKey;
        this.prescriptionUploaded = res.prescriptionUploaded ?? false;
        this.successMessage = "Reserva creada exitosamente. Guarda tu clave pública.";
        this.showSuccessModal = true;
        this.commonService.updateToastData("Reserva creada exitosamente", "success", "Éxito");
        this.resetForm();
      }
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

  resetForm(): void {
    this.reservationDetails = [];
    this.userName = "";
    this.userEmail = "";
    this.selectedPharmacyId = 0;
    this.availableDrugs = [];
    this.prescriptionBase64 = "";
    this.prescriptionFileName = "";
    this.prescriptionError = "";
  }
}
