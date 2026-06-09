namespace Admin.Blazor.Client.DTOs.Health
{
    public class ResourceStatusDTO
    {
        public string? ResourceName { get; set; }
        public bool ServiceAvailable { get; set; }
        public bool DatabaseAvailable { get; set; }
        public string? Message { get; set; }
        public string? Progress { get; set; }

        public void Reset()
        {
            ServiceAvailable = false;
            DatabaseAvailable = false;
            Message = null;
            Progress = null;
        }

    }
}
