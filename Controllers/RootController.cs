using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using restapi.Models;

namespace restapi.Controllers
{
    public class RootController : Controller
    {
        // GET api/values
        [Route("~/")]
        [HttpGet]
        [Produces(ContentTypes.Root)]
        [ProducesResponseType(typeof(IDictionary<ApplicationRelationship, DocumentLink>), 200)]
        public IDictionary<ApplicationRelationship, List<DocumentLink>> Get()
        {
            return new Dictionary<ApplicationRelationship, List<DocumentLink>>()
            {
                {
                    ApplicationRelationship.Timesheets, new List<DocumentLink>()
                    {
                        new DocumentLink()
                        {
                            Method = Method.Get,
                            Type = ContentTypes.Timesheets,
                            Relationship = DocumentRelationship.Timesheets,
                            Reference = "/timesheets"
                        },
                        new DocumentLink()
                        {
                            Method = Method.Post,
                            Type = ContentTypes.Timesheets,
                            Relationship = DocumentRelationship.CreateTimesheet,
                            Reference = "/timesheets" 
                        }
                    }
                }
            };
        }
    }
}