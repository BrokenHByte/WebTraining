using WebTraining.Models;

namespace WebTraining.Services;

public interface IEventService
{
    // READ
    List<Event> GetEvents();     
    Event GetEventById(int id); 
    
    // WRITE
    void AddEvent(Event data);
    void UpdateEvent(int id, Event data);    
    void DeleteEventById(int id); 
}

public class EventService : IEventService
{
    public List<Event> GetEvents()
    {
        throw new NotImplementedException();
    }

    public Event GetEventById(int id)
    {
        throw new NotImplementedException();
    }

    public void AddEvent(Event data)
    {
        throw new NotImplementedException();
    }

    public void UpdateEvent(int id, Event data)
    {
        throw new NotImplementedException();
    }

    public void DeleteEventById(int id)
    {
        throw new NotImplementedException();
    }
}

