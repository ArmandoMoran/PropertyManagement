using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Interfaces;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;

namespace PropertyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IPropertyRepository _propertyRepo;
    private readonly ILenderRepository _lenderRepo;
    private readonly IHoaRepository _hoaRepo;
    private readonly IInsuranceRepository _insuranceRepo;

    public PropertiesController(
        IReportService reportService,
        IPropertyRepository propertyRepo,
        ILenderRepository lenderRepo,
        IHoaRepository hoaRepo,
        IInsuranceRepository insuranceRepo)
    {
        _reportService = reportService;
        _propertyRepo = propertyRepo;
        _lenderRepo = lenderRepo;
        _hoaRepo = hoaRepo;
        _insuranceRepo = insuranceRepo;
    }

    [HttpGet]
    public async Task<ActionResult<List<PropertyListItemDto>>> GetProperties()
    {
        var properties = await _reportService.GetPropertyListAsync();
        return Ok(properties);
    }

    [HttpGet("years")]
    public async Task<ActionResult<List<int>>> GetAvailableYears()
    {
        var years = await _reportService.GetAvailableYearsAsync();
        return Ok(years);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PropertyDetailDto>> GetPropertyDetail(int id)
    {
        var property = await _propertyRepo.GetPropertyByIdAsync(id);
        if (property == null) return NotFound();

        var lender = await _lenderRepo.GetLenderByPropertyIdAsync(id);
        var allLenders = await _lenderRepo.GetAllLendersByPropertyIdAsync(id);
        var hoa = await _hoaRepo.GetHoaByPropertyIdAsync(id);
        var insurance = await _insuranceRepo.GetInsuranceByPropertyIdAsync(id);
        var premiums = await _insuranceRepo.GetAllPremiumsByPropertyAsync(id);
        var history = await _propertyRepo.GetPropertyHistoryAsync(id);

        var balanceHistory = new List<PrincipalBalanceDto>();
        var balances = await _lenderRepo.GetPrincipalBalanceHistoryAsync(id);
        balanceHistory = balances.Select(b => new PrincipalBalanceDto
        {
            BalanceId = b.BalanceId,
            PropertyId = b.PropertyId,
            SnapshotDate = b.SnapshotDate,
            PrincipalBalance = b.PrincipalBalance
        }).ToList();

        var dto = new PropertyDetailDto
        {
            PropertyId = property.PropertyId,
            FullAddress = property.FullAddress,
            Street = property.Street,
            City = property.City,
            State = property.State,
            ZipCode = property.ZipCode,
            Owner = property.Owner,
            PropertyType = property.PropertyType,
            Units = property.Units,
            SqFt = property.SqFt,
            Zestimate = property.Zestimate,
            Lender = lender == null ? null : new LenderDto
            {
                LenderId = lender.LenderId,
                PropertyId = lender.PropertyId,
                LenderName = lender.LenderName,
                LenderUrl = lender.LenderUrl,
                UserId = lender.UserId,
                MortgageNumber = lender.MortgageNumber,
                MonthlyPayment = lender.MonthlyPayment,
                EffectiveDate = lender.EffectiveDate
            },
            Hoa = hoa == null ? null : new HoaDto
            {
                HOAId = hoa.HOAId,
                PropertyId = hoa.PropertyId,
                HOAName = hoa.HOAName,
                AccountNumber = hoa.AccountNumber,
                ManagementCompany = hoa.ManagementCompany,
                PaymentFrequency = hoa.PaymentFrequency,
                PaymentAmount = hoa.PaymentAmount,
                EffectiveYear = hoa.EffectiveYear
            },
            Insurance = insurance == null ? null : new InsuranceDto
            {
                InsuranceId = insurance.InsuranceId,
                PropertyId = insurance.PropertyId,
                Carrier = insurance.Carrier,
                PolicyNumber = insurance.PolicyNumber,
                RenewalDate = insurance.RenewalDate,
                WhoPays = insurance.WhoPays
            },
            InsurancePremiums = premiums.Select(p => new InsurancePremiumDto
            {
                PremiumId = p.PremiumId,
                InsuranceId = p.InsuranceId,
                PolicyYear = p.PolicyYear,
                AnnualPremium = p.AnnualPremium,
                YOYPercentChange = p.YOYPercentChange
            }).ToList(),
            AllLenders = allLenders.Select(l => new LenderDto
            {
                LenderId = l.LenderId,
                PropertyId = l.PropertyId,
                LenderName = l.LenderName,
                LenderUrl = l.LenderUrl,
                UserId = l.UserId,
                MortgageNumber = l.MortgageNumber,
                MonthlyPayment = l.MonthlyPayment,
                EffectiveDate = l.EffectiveDate
            }).ToList(),
            PrincipalBalanceHistory = balanceHistory,
            PropertyHistory = history.Select(h => new PropertyHistoryDto
            {
                HistoryId = h.HistoryId,
                PropertyId = h.PropertyId,
                EventDate = h.EventDate,
                PropertyName = h.PropertyName,
                Description = h.Description,
                Notes = h.Notes,
                CreatedDate = h.CreatedDate
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult> CreateProperty([FromBody] Property property)
    {
        var id = await _propertyRepo.CreatePropertyAsync(property);
        property.PropertyId = id;
        return CreatedAtAction(nameof(GetPropertyDetail), new { id }, property);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateProperty(int id, [FromBody] Property property)
    {
        property.PropertyId = id;
        var ok = await _propertyRepo.UpdatePropertyAsync(property);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProperty(int id)
    {
        var ok = await _propertyRepo.DeletePropertyAsync(id);
        return ok ? NoContent() : NotFound();
    }

    // --- Lender endpoints ---
    [HttpGet("{propertyId}/lenders")]
    public async Task<ActionResult> GetLenders(int propertyId)
    {
        var lenders = await _lenderRepo.GetAllLendersByPropertyIdAsync(propertyId);
        return Ok(lenders);
    }

    [HttpPost("{propertyId}/lenders")]
    public async Task<ActionResult> CreateLender(int propertyId, [FromBody] Lender lender)
    {
        lender.PropertyId = propertyId;
        var id = await _lenderRepo.CreateLenderAsync(lender);
        lender.LenderId = id;
        return Created($"api/properties/{propertyId}/lenders", lender);
    }

    [HttpPut("lenders/{lenderId}")]
    public async Task<ActionResult> UpdateLender(int lenderId, [FromBody] Lender lender)
    {
        lender.LenderId = lenderId;
        var ok = await _lenderRepo.UpdateLenderAsync(lender);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("lenders/{lenderId}")]
    public async Task<ActionResult> DeleteLender(int lenderId)
    {
        var ok = await _lenderRepo.DeleteLenderAsync(lenderId);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("lenders/{lenderId}/balances")]
    public async Task<ActionResult> GetBalanceHistory(int lenderId)
    {
        // Get lender to find propertyId
        var balances = await _lenderRepo.GetPrincipalBalanceHistoryAsync(lenderId);
        return Ok(balances);
    }

    [HttpPost("{propertyId}/balances")]
    public async Task<ActionResult> CreateBalance(int propertyId, [FromBody] PrincipalBalanceHistory balance)
    {
        balance.PropertyId = propertyId;
        var id = await _lenderRepo.CreatePrincipalBalanceAsync(balance);
        balance.BalanceId = id;
        return Created($"api/properties/{propertyId}/balances", balance);
    }

    // --- HOA endpoints ---
    [HttpGet("{propertyId}/hoa")]
    public async Task<ActionResult> GetHoa(int propertyId)
    {
        var hoa = await _hoaRepo.GetAllHoaByPropertyIdAsync(propertyId);
        return Ok(hoa);
    }

    [HttpPost("{propertyId}/hoa")]
    public async Task<ActionResult> CreateHoa(int propertyId, [FromBody] HoaInfo hoa)
    {
        hoa.PropertyId = propertyId;
        var id = await _hoaRepo.CreateHoaAsync(hoa);
        hoa.HOAId = id;
        return Created($"api/properties/{propertyId}/hoa", hoa);
    }

    [HttpPut("hoa/{hoaId}")]
    public async Task<ActionResult> UpdateHoa(int hoaId, [FromBody] HoaInfo hoa)
    {
        hoa.HOAId = hoaId;
        var ok = await _hoaRepo.UpdateHoaAsync(hoa);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("hoa/{hoaId}")]
    public async Task<ActionResult> DeleteHoa(int hoaId)
    {
        var ok = await _hoaRepo.DeleteHoaAsync(hoaId);
        return ok ? NoContent() : NotFound();
    }

    // --- Insurance endpoints ---
    [HttpGet("{propertyId}/insurance")]
    public async Task<ActionResult> GetInsurance(int propertyId)
    {
        var insurance = await _insuranceRepo.GetInsuranceByPropertyIdAsync(propertyId);
        if (insurance == null) return Ok(new { });
        var premiums = await _insuranceRepo.GetAllPremiumsByPropertyAsync(propertyId);
        return Ok(new { insurance, premiums });
    }

    [HttpPost("{propertyId}/insurance")]
    public async Task<ActionResult> CreateInsurance(int propertyId, [FromBody] Insurance insurance)
    {
        insurance.PropertyId = propertyId;
        var id = await _insuranceRepo.CreateInsuranceAsync(insurance);
        insurance.InsuranceId = id;
        return Created($"api/properties/{propertyId}/insurance", insurance);
    }

    [HttpPut("insurance/{insuranceId}")]
    public async Task<ActionResult> UpdateInsurance(int insuranceId, [FromBody] Insurance insurance)
    {
        insurance.InsuranceId = insuranceId;
        var ok = await _insuranceRepo.UpdateInsuranceAsync(insurance);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("insurance/{insuranceId}/premiums")]
    public async Task<ActionResult> CreatePremium(int insuranceId, [FromBody] InsurancePremium premium)
    {
        premium.InsuranceId = insuranceId;
        var id = await _insuranceRepo.CreatePremiumAsync(premium);
        premium.PremiumId = id;
        return Created($"api/properties/insurance/{insuranceId}/premiums", premium);
    }
}
