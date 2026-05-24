export interface ReservationRequest {
    pharmacyId: number;
    userEmail: string;
    details: ReservationDetailRequest[];
    prescriptionBase64?: string;
    prescriptionFileName?: string;
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
    expirationDate: string;
    status: string;
    details: ReservationDetailResponse[];
    prescriptionUploaded?: boolean;
    hasRecipe: boolean;
    requiresPrescription: boolean;
    recipeFiles: string[];
}

export interface ReservationDetailResponse {
    id: number;
    drugCode: string;
    drugName?: string;
    quantity: number;
    requiresPrescription: boolean;
}
