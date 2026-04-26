using WebTraining.Models;

namespace WebTraining.Services;

public interface IEventService
{
    // READ
    List<Event> GetEvents();     
    Event? GetEventById(int id); 
    
    // WRITE
    bool AddEvent(Event data);
    bool UpdateEvent(int id, Event data);    
    bool DeleteEventById(int id); 
}

// Синглтоновский сервис
public class EventService : IEventService
{
    private List<Event> _events = new List<Event>();
    private Lock _lock = new ();
    
    public List<Event> GetEvents()
    {
        lock (_lock)
        {
            return _events.ToList();           
        }
    }

    public Event? GetEventById(int id)
    {
        lock (_lock)
        {
            return _events.FirstOrDefault(e => e.Id == id);           
        }
    }

    public bool AddEvent(Event data)
    {
        lock (_lock)
        {
            if (_events.Find(x => x.Id == data.Id) == null)
            {
                _events.Add(data);
                return true;
            }
            return false;
        }
    }

    public bool UpdateEvent(int id, Event data)
    {
        lock (_lock)
        {
            int index = _events.FindIndex(x => x.Id == id);
            if (index != -1)
            {
                _events[index] = data;
                return true;
            }
            return false;
        }
    }

    public bool DeleteEventById(int id)
    {
        lock (_lock)
        {
            return _events.RemoveAll(x => x.Id == id) > 0;
        }
    }
}

