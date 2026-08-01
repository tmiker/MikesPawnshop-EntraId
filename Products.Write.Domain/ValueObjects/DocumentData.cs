using Products.Write.Domain.Base;
using Products.Write.Domain.Snapshots;

namespace Products.Write.Domain.ValueObjects
{
    public class DocumentData : ValueObject
    {
        public string? Name { get; private set; }           // for Azure blob storage, virtual directory plus filename
        public string? Title { get; private set; }
        public int SequenceNumber { get; private set; }
        public string? DocumentUrl { get; private set; }

        public DocumentData() { }

        public DocumentData(string name, string title, int sequenceNumber, string documentUrl)
        {
            Name = name;
            Title = title;
            SequenceNumber = sequenceNumber;
            DocumentUrl = documentUrl;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name!;
            yield return Title!;
            yield return SequenceNumber;
            yield return DocumentUrl!;
        }
        public DocumentDataSnapshot GetSnapshot()
        {
            return new DocumentDataSnapshot(Name, Title, SequenceNumber, DocumentUrl);
        }
    }
}
