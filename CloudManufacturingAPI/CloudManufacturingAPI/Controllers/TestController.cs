using CloudManufacturingAPI.Repositories.SystemManagement;
using CloudManufacturingSharedLibrary;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace CloudManufacturingAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ISystemRepository _systemRepository;

        public TestController(ISystemRepository systemRepository)
        {
            _systemRepository = systemRepository;
        }

        [HttpGet]
        [Route("TestEndpoint")]
        public ActionResult TestEndpoint([FromQuery] string orderId)
        {
            try
            {
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("RunMonitoring")]
        public ActionResult RunMonitoring()
        {
            try
            {
                _systemRepository.RunMonitoring();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetSchedulingMethod")]
        public ActionResult GetSchedulingMethod()
        {
            try
            {
                return Ok(_systemRepository.GetSchedulingMethod());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("SetSchedulingMethod")]
        public ActionResult SetSchedulingMethod(int schedulingMethod)
        {
            try
            {
                _systemRepository.SetSchedulingMethod((Constants.SCHEDULING_METHOD)schedulingMethod);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("ClearSystemMemory")]
        public ActionResult ClearSystemMemory([FromQuery]bool setDefaultSchedulingMethod = false)
        {
            try
            {
                _systemRepository.ClearSystemMemory(setDefaultSchedulingMethod);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
