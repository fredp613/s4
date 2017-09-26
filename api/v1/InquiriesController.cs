using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MasterProject.Data;
using MasterProject.Models;
using Microsoft.Xrm.Sdk;
using MasterProject.CRM;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MasterProject.api.v1
{
    [Produces("application/json")]
    [Route("api/v1/Inquiries")]
    public class InquiriesController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IOrganizationService _crmService;

        public InquiriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
            _crmService = CrmService.GetServiceProvider();
        }

        // GET: api/Inquiries
        [HttpGet]
        public IEnumerable<Inquiry> GetInquiry()
        {
            return _context.Inquiry;
        }
        [HttpGet("conn")]
        public string GetConn()
        {
           // Debug.WriteLine("asdfsdfsfsdffads");
       
            return CrmService.GetTestUserInfo();
        }

        // GET: api/Inquiries/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInquiry([FromRoute] Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var inquiry = await _context.Inquiry.SingleOrDefaultAsync(m => m.InquiryId == id);

            if (inquiry == null)
            {
                return NotFound();
            }

            return Ok(inquiry);
        }

        // PUT: api/Inquiries/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInquiry([FromRoute] Guid id, [FromBody] Inquiry inquiry)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != inquiry.InquiryId)
            {
                return BadRequest();
            }

            _context.Entry(inquiry).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InquiryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InquiryId,Content,Response,ApplicationUserId,ResponseBy")] Inquiry inquiry)
        {
            if (ModelState.IsValid)
            {
                inquiry.InquiryId = Guid.NewGuid();
                inquiry.ApplicationUserId = _userManager.GetUserId(User);
                _context.Add(inquiry);
                await _context.SaveChangesAsync();

                var newCrmInquiry = new Entity("fp_inquiry");

                QueryExpression qe = new QueryExpression("contact");
                qe.Criteria.AddCondition("fp_portalid", ConditionOperator.Equal, inquiry.ApplicationUserId);
                Guid crmContactId = _crmService.RetrieveMultiple(qe).Entities.First().Id;

                newCrmInquiry.Id = inquiry.InquiryId;
                newCrmInquiry["fp_name"] = _userManager.GetUserName(User);
                newCrmInquiry["fp_question"] = inquiry.Content;
                newCrmInquiry["fp_contact"] = new EntityReference("contact", crmContactId);
                _crmService.Create(newCrmInquiry);

                return RedirectToAction("Index");
            }


            ViewData["ApplicationUserId"] = new SelectList(_context.Users, "Id", "Id", inquiry.ApplicationUserId);
            return View(inquiry);
        }

        // DELETE: api/Inquiries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInquiry([FromRoute] Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var inquiry = await _context.Inquiry.SingleOrDefaultAsync(m => m.InquiryId == id);
            if (inquiry == null)
            {
                return NotFound();
            }

            _context.Inquiry.Remove(inquiry);
            await _context.SaveChangesAsync();

            return Ok(inquiry);
        }

        private bool InquiryExists(Guid id)
        {
            return _context.Inquiry.Any(e => e.InquiryId == id);
        }
    }
}