using Products.Write.Domain.Base;
using Products.Write.Domain.Enumerations;
using Products.Write.Domain.Events;
using Products.Write.Domain.Snapshots;
using Products.Write.Domain.ValueObjects;

namespace Products.Write.Domain.Aggregates
{
    public class Product : Entity, IAggregateRoot
    {
        private string? _name;
        private string? _category;
        private string? _description;
        private decimal _price;
        private string? _currency;
        private string? _status;
        private List<ImageData>? _images;
        private List<DocumentData>? _documents;
        // inventory awareness
        private int _quantityOnHand;
        private int _quantityAllocated;
        private string? _uom;
        private int _lowStockThreshold;
        private int _version;
        private DateTime _dateCreated;
        private DateTime _dateUpdated;

        private Product() { }

        public Product(ProductSnapshot snapshot)
        {
            List<ImageData> images = new List<ImageData>();
            snapshot.Images?.ForEach(i => images.Add(new ImageData(i.Name!, i.Caption!, i.SequenceNumber, i.ImageUrl!, i.ThumbnailUrl!)));
            List<DocumentData> documents = new List<DocumentData>();
            snapshot.Documents?.ForEach(d => documents.Add(new DocumentData(d.Name!, d.Title!, d.SequenceNumber, d.DocumentUrl!)));
            Id = snapshot.Id;
            _name = snapshot.Name;
            _category = snapshot.Category;
            _description = snapshot.Description;
            _price = snapshot.Price;
            _currency = snapshot.Currency;
            _status = snapshot.Status;
            _version = snapshot.Version;
            _dateCreated = snapshot.DateCreated;
            _dateUpdated = snapshot.DateUpdated;
            _quantityOnHand = snapshot.QuantityOnHand;
            _quantityAllocated = snapshot.QuantityAllocated;
            _lowStockThreshold = snapshot.LowStockThreshold;
            _uom = snapshot.UOM;            
        }

        public Product(IEnumerable<IDomainEvent> domainEvents)
        {
            foreach (IDomainEvent domainEvent in domainEvents)
            {
                Apply(domainEvent);
            }
        }

        private void Causes(IDomainEvent @event)
        {
            AddDomainEvent(@event);
            Apply(@event);
        }

        public void Apply(IDomainEvent @event)
        {
            When((dynamic)@event);
            // _version++;
        }

        public Product(string name, CategoryEnum category, string description, decimal price, string currency, string status, 
            int quantityOnHand, string uom, int lowStockThreshold, string correlationId)
        {
            // validate status - below statement will throw InvalidEnumArgumentException if not valid
            Status validStatus = Status.FromName(status);
            // event constructor takes category enum and converts to string value for the property
            Causes(new ProductAdded(Guid.NewGuid(), this.GetType().Name, _version, correlationId, name, category, description, price, currency, validStatus.Name,
                quantityOnHand, 0, uom, lowStockThreshold));
        }

        private void When(ProductAdded @event)
        {
            Id = @event.AggregateId;
            _name = @event.Name;
            _category = @event.Category;
            _description = @event.Description;
            _price = @event.Price;
            _currency = @event.Currency;
            _status = @event.Status;
            _quantityOnHand = @event.QuantityOnHand;
            _quantityAllocated = 0;
            _uom = @event.UOM;
            _lowStockThreshold = @event.LowStockThreshold;
            _version = @event.AggregateVersion;
            _dateCreated = @event.OccurredAt;
            _dateUpdated = @event.OccurredAt;
        }

        public void UpdateStatus(string statusName, string correlationId)
        {
            // validate valid status name from enumeration - event constructor takes enumeration and assigns its name value to event property
            Status validStatus = Status.FromName(statusName);
            Causes(new StatusUpdated(Id, this.GetType().Name, _version + 1, correlationId, validStatus.Name));
        }

        private void When(StatusUpdated @event)
        {
            _status = @event.Status;
            _version = @event.AggregateVersion;
            _dateUpdated = @event.OccurredAt;
        }

        public void AddImage(string name, string caption, string imageUrl, string thumbnailUrl, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(caption) || string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                throw new ArgumentNullException("Missing Image Metadata; Caption, Image URL, and Thumbnail URL are required.");
            }
            int maxSequenceNumber = MaxImageSequenceNumber;
            Causes(new ImageAdded(Id, this.GetType().Name, _version + 1, correlationId, name, caption, maxSequenceNumber + 1, imageUrl, thumbnailUrl));
        }

        private void When(ImageAdded @event)
        {
            ImageData image = new ImageData(@event.Name, @event.Caption!, @event.SequenceNumber, @event.ImageUrl!, @event.ThumbnailUrl!);
            if (_images is null) _images = new List<ImageData>();
            _images.Add(image);
            _version = @event.AggregateVersion;
            _dateUpdated = @event.OccurredAt;
        }

        public void AddDocument(string name, string title, string documentUrl, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(documentUrl))
            {
                throw new ArgumentNullException("Missing Document Metadata; Title and Document URL are required.");
            }
            int maxSequenceNumber = MaxDocumentSequenceNumber;
            Causes(new DocumentAdded(Id, this.GetType().Name, _version + 1, correlationId, name, title, maxSequenceNumber + 1, documentUrl));
        }

        private void When(DocumentAdded @event)
        {
            DocumentData doc = new DocumentData(@event.Name, @event.Title!, @event.SequenceNumber, @event.DocumentUrl!);
            if (_documents is null) _documents = new List<DocumentData>();
            _documents.Add(doc);
            _version = @event.AggregateVersion;
            _dateUpdated = @event.OccurredAt;
        }

        public void DeleteImage(string filename, string? correlationId)
        {
            Causes(new ImageDeleted(Id, this.GetType().Name, _version + 1, correlationId, filename));
        }

        public void When(ImageDeleted @event)
        {
            if (_images is null) return;
            ImageData? image = _images.FirstOrDefault(d => d.Name == @event.FileName);
            if (image is not null) _images.Remove(image);
        }

        public void DeleteDocument(string filename, string? correlationId)
        {
            Causes(new DocumentDeleted(Id, this.GetType().Name, _version + 1, correlationId, filename));
        }

        public void When(DocumentDeleted @event)
        {
            if (_documents is null) return;
            DocumentData? doc = _documents.FirstOrDefault(d => d.Name == @event.FileName);
            if (doc is not null) _documents.Remove(doc);
        }

        private int MaxImageSequenceNumber
        {
            get
            {
                if (_images is not null && _images.Any()) return _images.Max(i => i.SequenceNumber);
                return 0;
            }
        }

        private int MaxDocumentSequenceNumber
        {
            get
            {
                if (_documents is not null && _documents.Any()) return _documents.Max(i => i.SequenceNumber);
                return 0;
            }
        }

        public bool ImageFileNameExists(string filename) => _images is null ? false : _images.Where(d => d.Name == filename).Count() > 0;
        public bool DocumentFileNameExists(string filename) => _documents is null ? false : _documents.Where(d => d.Name == filename).Count() > 0;

        public ProductSnapshot GetSnapshot()
        {
            List<ImageDataSnapshot> imageSnapshots = new List<ImageDataSnapshot>();
            _images?.ForEach(i => imageSnapshots.Add(i.GetSnapshot()));
            List<DocumentDataSnapshot> documentSnapshots = new List<DocumentDataSnapshot>();
            _documents?.ForEach(d => documentSnapshots.Add(d.GetSnapshot()));

            return new ProductSnapshot()
            {
                Id = Id,
                Name = _name,
                Category = _category,
                Description = _description,
                Price = _price,
                Currency = _currency,
                Status = _status,
                Images = imageSnapshots,
                Documents = documentSnapshots,
                QuantityOnHand = _quantityOnHand,
                QuantityAllocated = _quantityAllocated,
                UOM = _uom,
                LowStockThreshold = _lowStockThreshold,
                Version = _version,
                DateCreated = _dateCreated,
                DateUpdated = _dateUpdated,
            };
        }
    }
}
