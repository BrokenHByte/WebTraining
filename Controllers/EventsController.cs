using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WebTraining.Models;

namespace WebTraining.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new[] { "User1", "User2", "User3" });
    }  
    
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok($"Event with ID: {id}");
    }
    
    [HttpPost]
    public IActionResult CreateEvent([FromBody] Event data)
    {
        return Created();
    }   
    
    [HttpPut("{id}")]
    public IActionResult UpdateEvent(int id, [FromBody] Event data)
    {
        return Ok();
    }  
  
    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(int id)
    {
        return Accepted();
    }    
}