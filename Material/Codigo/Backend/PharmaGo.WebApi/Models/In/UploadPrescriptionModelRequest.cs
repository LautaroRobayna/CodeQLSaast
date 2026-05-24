namespace PharmaGo.WebApi.Models.In
{
    public class UploadPrescriptionModelRequest
    {
        public string PrescriptionBase64 { get; set; } = string.Empty;
        public string PrescriptionFileName { get; set; } = string.Empty;
    }
}
