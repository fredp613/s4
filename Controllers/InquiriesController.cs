using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MasterProject.Data;
using MasterProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MasterProject.CRM;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MasterProject.Controllers
{
    [Authorize]
    public class InquiriesController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IOrganizationService _crmService;

        public InquiriesController(ApplicationDbContext context,UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
            _crmService = CrmService.GetServiceProvider();
        }

        // GET: Inquiries
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Inquiry.Include(i => i.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Inquiries/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var inquiry = await _context.Inquiry
                .Include(i => i.User)
                .SingleOrDefaultAsync(m => m.InquiryId == id);
            if (inquiry == null)
            {
                return NotFound();
            }
            var crmInquiry = _crmService.Retrieve("fp_inquiry", inquiry.InquiryId, new ColumnSet("fp_response"));
            if (crmInquiry != null)
            {
                inquiry.Response = crmInquiry.GetAttributeValue<string>("fp_response");
            }
            


            return View(inquiry);
        }

        // GET: Inquiries/Create
        public IActionResult Create()
        {
            ViewData["ApplicationUserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Inquiries/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: Inquiries/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inquiry = await _context.Inquiry.SingleOrDefaultAsync(m => m.InquiryId == id);
            if (inquiry == null)
            {
                return NotFound();
            }
            ViewData["ApplicationUserId"] = new SelectList(_context.Users, "Id", "Id", inquiry.ApplicationUserId);
            return View(inquiry);
        }

        // POST: Inquiries/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("InquiryId,Content,Response,ApplicationUserId,ResponseBy")] Inquiry inquiry)
        {
            if (id != inquiry.InquiryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inquiry);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InquiryExists(inquiry.InquiryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            ViewData["ApplicationUserId"] = new SelectList(_context.Users, "Id", "Id", inquiry.ApplicationUserId);
            return View(inquiry);
        }

        // GET: Inquiries/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inquiry = await _context.Inquiry
                .Include(i => i.User)
                .SingleOrDefaultAsync(m => m.InquiryId == id);
            if (inquiry == null)
            {
                return NotFound();
            }

            return View(inquiry);
        }

        // POST: Inquiries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var inquiry = await _context.Inquiry.SingleOrDefaultAsync(m => m.InquiryId == id);
            _context.Inquiry.Remove(inquiry);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool InquiryExists(Guid id)
        {
            return _context.Inquiry.Any(e => e.InquiryId == id);
        }
    }
}
