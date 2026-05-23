export interface ReservationRequest {
    pharmacyId: number;
    userEmail: string;
    details: ReservationDetailRequest[];
}

export interface ReservationDetailRequest {
    drugCode: string;
    quantity: number;
}

export interface ReservationResponse {
    id: number;
    code: string;
    publicKey: string;
    pharmacyId: number;
    userEmail: string;
    reservationDate: string;
    status: string;
    details: ReservationDetailResponse[];
}

export interface ReservationDetailResponse {
    id: number;
    drugCode: string;
    drugName?: string;
    quantity: number;
    requiresPrescription: boolean;
}
