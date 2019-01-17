using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using restapi.Models;

namespace restapi.Controllers
{
    [Route("[controller]")]
    public class TimesheetsController : Controller
    {
        [HttpGet]
        [Produces(ContentTypes.Timesheets)]
        [ProducesResponseType(typeof(IEnumerable<Timecard>), 200)]
        public IEnumerable<Timecard> GetAll()
        {
            return Database
                .All
                .OrderBy(t => t.Opened);
        }

        //GET a Timecard using ID
        [HttpGet("{id}")]
        [Produces(ContentTypes.Timesheet)]
        [ProducesResponseType(typeof(Timecard), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetOne(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null) 
            {
                return Ok(timecard);
            }
            else
            {
                return NotFound();
            }
        }

        //POST a new Timecard with Resource from Data
        [HttpPost]
        [Produces(ContentTypes.Timesheet)]
        [ProducesResponseType(typeof(Timecard), 200)]
        public Timecard Create([FromBody] DocumentResource resource)
        {
            var timecard = new Timecard(resource.Resource);

            var entered = new Entered() { Resource = resource.Resource };

            timecard.Transitions.Add(new Transition(entered));

            Database.Add(timecard);

            return timecard;
        }

        //GET a specific timecard and contents of array sorted by workdate then recorded 
        [HttpGet("{id}/lines")]
        [Produces(ContentTypes.TimesheetLines)]
        [ProducesResponseType(typeof(IEnumerable<AnnotatedTimecardLine>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetLines(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {   //Removed sort so Array indexes correspond with printout array order
                var lines = timecard.Lines;
                    //.OrderBy(l => l.Recorded)
                    //.ThenBy(l => l.WorkDate);

                return Ok(lines);
            }
            else
            {
                return NotFound();
            }
        }

        //POST timecard lines, using information from BODY add in WEEK/YEAR//HOURS/PROJECT
        [HttpPost("{id}/lines")]
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(AnnotatedTimecardLine), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        public IActionResult AddLine(string id, [FromBody] TimecardLine timecardLine)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                var annotatedLine = timecard.AddLine(timecardLine);

                return Ok(annotatedLine);
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpGet("{id}/transitions")]
        [Produces(ContentTypes.Transitions)]
        [ProducesResponseType(typeof(IEnumerable<Transition>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetTransitions(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                return Ok(timecard.Transitions);
            }
            else
            {
                return NotFound();
            }
        }

        //POST timecard to submit for approval,  takes ID from address and submit from body
        [HttpPost("{id}/submittal")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(NotAuthorizedError), 409)]
        public IActionResult Submit(string id, [FromBody] Submittal submittal)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                if (timecard.Lines.Count < 1)
                {
                    return StatusCode(409, new EmptyTimecardError() { });
                }
                if(timecard.Resource == submittal.Resource)
                {
                    var transition = new Transition(submittal, TimecardStatus.Submitted);
                    timecard.Transitions.Add(transition);
                    return Ok(transition);
                }
                else
                {
                    return StatusCode(409, new NotAuthorizedError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        //GET timecard that has been submitted
        [HttpGet("{id}/submittal")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetSubmittal(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Submitted)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Submitted)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);                                        
                }
                else 
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        //POST for cancellation if it is not a draft and not submitted
        [HttpPost("{id}/cancellation")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(NotAuthorizedError), 409)]
        public IActionResult Cancel(string id, [FromBody] Cancellation cancellation)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft && timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }
                //Only you can cancel your timecard
                if(timecard.Resource == cancellation.Resource)
                {
                    var transition = new Transition(cancellation, TimecardStatus.Cancelled);
                    timecard.Transitions.Add(transition);
                    return Ok(transition);
                }
                else
                {
                    return StatusCode(409, new NotAuthorizedError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/cancellation")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetCancellation(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Cancelled)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Cancelled)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);                                        
                }
                else 
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/rejection")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(NotAuthorizedError), 409)]
        public IActionResult Close(string id, [FromBody] Rejection rejection)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }
                //Can't reject your own timecard
                if(timecard.Resource != rejection.Resource){
                    var transition = new Transition(rejection, TimecardStatus.Rejected);
                    timecard.Transitions.Add(transition);
                    return Ok(transition);
                }
                else
                {
                    return StatusCode(409, new NotAuthorizedError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/rejection")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetRejection(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Rejected)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Rejected)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);                                        
                }
                else 
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpPost("{id}/approval")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(NotAuthorizedError), 409)]
 
        public IActionResult Approve(string id, [FromBody] Approval approval)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }
                //HW #5, Verify approver is not timecard resource
                if(timecard.Resource != approval.Resource){
                    var transition = new Transition(approval, TimecardStatus.Approved);
                    timecard.Transitions.Add(transition);
                    return Ok(transition);
                }
                else
                {
                    return StatusCode(409, new NotAuthorizedError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/approval")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetApproval(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Approved)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Approved)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);                                        
                }
                else 
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }  

        //HW #1 Delete a Draft or Cancelled Timecard
        [HttpDelete("{id}")]
        [Produces(ContentTypes.Transition)]         //Swagger can determin this stuff by sniffing it out
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingDeletionError), 409)]
        public IActionResult Delete(string id)
        {   //Find timecard in database
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Cancelled || timecard.Status == TimecardStatus.Draft)
                {
                    Database.Delete(id);
                    return Ok();        //Return 200
                }
                else
                {
                    return StatusCode(409, new MissingDeletionError() { });
                }
            }
            else
            {
                return NotFound();  //Return 404
            }
        } 

        //Prof code
        //First param checks to see if timecard is even real
        [HttpPost("{timecardID}/lines/{lineID}")]
        public IActionResult UpdateLin(string timecardID, string lineID, [FromBody] TimecardLine timecardLine)
        {
            Timecard timecard = Database.Find(timecardID);

            if (timecard ==  null){
                return NotFound();
            }

            //does this line exist?
            return Ok();
        }


        //HW #2 Replace(POST) a complete line item
        //By this I am assuming you mean all the values in an index (week,year,day, hours, project)
        //{ArrayIndex} starts at 0 and increments by 1 for each day added to timecard
        [HttpPost("{id}/lines/{ArrayIndex}")]
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(AnnotatedTimecardLine), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        public IActionResult ReplaceLine(string id, int ArrayIndex, [FromBody] TimecardLine timecardLine)
        {
            Timecard timecard = Database.Find(id);
    
            if(timecard != null) 
            {
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                var annotatedLine = timecard.ReplaceLine(ArrayIndex, timecardLine);
                return Ok(annotatedLine);
            }
            else
            {
                return NotFound();
            }
        }
        
        //HW#3 Update (PATCH) a line item
        //PATCH basic framework, it works and keeps same GUID, but if changing any of the datetime features
        //It doesn't change the workday.  Need to revise so that it will correct for this. Possibly move logic to Timecardline?
        [HttpPatch("{id}/lines/{ArrayIndex}")]
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(AnnotatedTimecardLine), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
         public IActionResult UpdateLine(string id, int ArrayIndex, [FromBody] TimecardLine timecardLine)
        {
            Timecard timecard = Database.Find(id);
                
            if(timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(408, new InvalidStateError() { });
                }

                var update = timecard.UpdateLine(ArrayIndex, timecardLine);

                return Ok(update);
            }
            else
            {
                return NotFound();
            }
        }
    }

    
}
