using CloudManufacturingAPI.Repositories.Machine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CloudManufacturingAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MachineController : ControllerBase
    {
        private readonly ILogger<MachineController> _logger;
        private readonly IMachineRepository _repository;

        public MachineController(ILogger<MachineController> logger, IMachineRepository machineRepository)
        {
            _logger = logger;
            _repository = machineRepository;
        }

        [HttpGet]
        [Route("HealthCheck")]
        public ActionResult HealthCheck()
        {
            return Ok();
        }

        [HttpGet]
        [Route("GetMachines")]
        public ActionResult Get()
        {
            try
            {
                return Ok(_repository.Get());
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("CreateMachine")]
        public ActionResult Create([FromBody]Models.Machine.MachineCreationDTO item)
        {
            try
            {
                return Ok(_repository.Create(item));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
