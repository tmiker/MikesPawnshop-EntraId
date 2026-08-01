namespace Products.Write.Application.Abstractions
{
    public interface IRegisterableEventHandlers
    {
        void RegisterWithEventAggregator(IEventAggregator eventAggregator);
    }
}
