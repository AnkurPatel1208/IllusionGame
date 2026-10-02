public interface IEventListener<in T> where T : struct {
    void OnEventRaised(T eventData);
}
