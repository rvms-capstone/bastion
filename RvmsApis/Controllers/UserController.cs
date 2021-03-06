using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using RvmsModels;

namespace UserApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly CosmosDbService _cosmosDbService;

    public UserController(ILogger<UserController> logger, CosmosDbService cosmosDbService)
    {
        _logger = logger;
        _cosmosDbService = cosmosDbService;
    }

    /// <summary>
    /// Get a registered patient.
    /// </summary>
    /// <param name="email">User email</param>
    [HttpGet("patient/{email}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Patient))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatient([EmailAddress] string email)
    {
        var patient = await _cosmosDbService.GetPatientByEmail(email);
        if (patient != null)
        {
            return Ok(patient);
        }

        return NotFound();
    }

    /// <summary>
    /// Get a registered healthcare practitioner.
    /// </summary>
    /// <param name="email">User email</param>
    [HttpGet("hcp/{email}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HealthcarePractitioner))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHealthcarePractitioner([EmailAddress] string email)
    {
        var healthcarePractitioner = await _cosmosDbService.GetHealthcarePractitionerByEmail(email);
        if (healthcarePractitioner != null)
        {
            return Ok(healthcarePractitioner);
        }

        return NotFound();
    }

    /// <summary>
    /// Register a new patient.
    /// </summary>
    /// <param name="registerUser">Request body containing user data.</param>
    [HttpPost("auth/patient/register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterUser registerUser)
    {
        try
        {
            Patient newPatient =
                await _cosmosDbService.RegisterPatient(registerUser);
            return Created($"/api/user/patient/{newPatient.Email}", newPatient);
        }
        catch (CosmosException ce)
        {
            if (ce.StatusCode == HttpStatusCode.Conflict)
            {
                return Conflict();
            }

            throw;
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Register a new healthcare practitioner.
    /// </summary>
    /// <param name="registerUser">Request body containing user data.</param>
    [HttpPost("auth/hcp/register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterHealthcarePractitioner([FromBody] RegisterUser registerUser)
    {
        try
        {
            HealthcarePractitioner newHcp =
                await _cosmosDbService.RegisterHealthcarePractitioner(registerUser);
            return Created($"/api/user/hcp/{newHcp.Email}", newHcp);
        }
        catch (CosmosException ce)
        {
            if (ce.StatusCode == HttpStatusCode.Conflict)
            {
                return Conflict();
            }

            throw;
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Patient Login
    /// </summary>
    /// <param name="logInUser">Email and password</param>
    /// <returns></returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid login credentials</response>
    /// <response code="404">User not found</response>
    [HttpPost("auth/patient/login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Patient))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogInPatient([FromBody] LogInUser logInUser)
    {
        var patient = await _cosmosDbService.GetPatientByEmail(logInUser.Email);
        if (patient == null)
        {
            return NotFound();
        }

        if (Security.ValidatePassword(patient.Salt, patient.Password, logInUser.Password))
        {
            return Ok(patient);
        }

        return Unauthorized();
    }

    /// <summary>
    /// Healthcare Practitioner Login
    /// </summary>
    /// <param name="logInUser">Email and password</param>
    /// <returns></returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid login credentials</response>
    /// <response code="404">User not found</response>
    [HttpPost("auth/hcp/login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Patient))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogInHealthcarePractitioner([FromBody] LogInUser logInUser)
    {
        var healthcarePractitioner = await _cosmosDbService.GetHealthcarePractitionerByEmail(logInUser.Email);
        if (healthcarePractitioner == null)
        {
            return NotFound();
        }

        if (Security.ValidatePassword(healthcarePractitioner.Salt, healthcarePractitioner.Password, logInUser.Password))
        {
            return Ok(healthcarePractitioner);
        }

        return Unauthorized();
    }

    [HttpGet("hcp/{hcpEmail}/patients")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHealthcarePractitionerPatients([EmailAddress] string hcpEmail)
    {
        var healthcarePractitioner = await _cosmosDbService.GetHealthcarePractitionerByEmail(hcpEmail);
        if (healthcarePractitioner != null)
        {
            return Ok(healthcarePractitioner.Patients);
        }

        return NotFound();
    }

    [HttpPut("hcp/{hcpEmail}/patients/{patientEmail}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HealthcarePractitioner))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPatientToHcp([EmailAddress] string hcpEmail,
        [EmailAddress] string patientEmail)
    {
        var patientToAdd = await _cosmosDbService.GetPatientByEmail(patientEmail);
        if (patientToAdd == null)
        {
            return NotFound($"Patient with email {patientEmail} not found.");
        }

        var healthcarePractitioner = await _cosmosDbService.GetHealthcarePractitionerByEmail(hcpEmail);
        if (healthcarePractitioner == null)
        {
            return NotFound($"Healthcare Practitioner with email {hcpEmail} not found.");
        }

        if (healthcarePractitioner.Patients.Contains(patientToAdd.Email, StringComparer.InvariantCultureIgnoreCase))
        {
            return Conflict($"Patient {patientEmail} is already registered with Healthcare Practitioner {hcpEmail}.");
        }

        var updatedHcp = await _cosmosDbService.AddPatientToHcp(healthcarePractitioner, patientEmail);
        if (updatedHcp == null)
        {
            return Conflict($"Unable to add patient {patientEmail} to HCP account.");
        }

        return Ok(updatedHcp);
    }

    [HttpDelete("hcp/{hcpEmail}/patients/{patientEmail}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Patient))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([EmailAddress] string hcpEmail, [EmailAddress] string patientEmail)
    {
        var patient = await _cosmosDbService.GetPatientByEmail(patientEmail);
        if (patient == null)
        {
            return NotFound($"Patient {patientEmail} not found.");
        }

        var hcp = await _cosmosDbService.GetHealthcarePractitionerByEmail(hcpEmail);
        if (hcp == null)
        {
            return NotFound($"Healthcare Practitioner {hcpEmail} not found.");
        }

        if (!hcp.Patients.Contains(patientEmail, StringComparer.InvariantCultureIgnoreCase))
        {
            return Conflict($"Patient {patientEmail} is not registered with Healthcare Practitioner {hcpEmail}.");
        }

        var updatedHcp = await _cosmosDbService.RemovePatientFromHcp(hcp, patientEmail);
        if (updatedHcp == null)
        {
            return Conflict($"Unable to add Patient {patientEmail} to Healthcare Practitioner {hcpEmail}");
        }

        return Ok(updatedHcp);
    }
}