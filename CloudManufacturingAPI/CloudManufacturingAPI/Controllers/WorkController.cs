using CloudManufacturingAPI.Models.Work;
using CloudManufacturingAPI.Repositories.Work;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace CloudManufacturingAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkController : ControllerBase
    {
        private readonly IWorkRepository _repository;

        public WorkController(IWorkRepository workRepository)
        {
            _repository = workRepository;
        }

        [HttpPost]
        [Route("AddWork")]
        public ActionResult AddWork([FromBody] AddWorkDTO newWork)
        {
            try
            {
                return Ok(_repository.AddWork(new List<AddWorkDTO>() { newWork }));
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

        [HttpPost]
        [Route("AddCompoundWork")]
        public ActionResult AddCompoundWork([FromBody] List<AddWorkDTO> newWork)
        {
            try
            {
                return Ok(_repository.AddWork(newWork));
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
        [Route("GetOrders")]
        public ActionResult GetOrders(int? start = null, int? length = null, string order = null, string orderId = null)
        {
            try
            {
                var result = _repository.GetOrders(start, length, order,orderId, out int filteredRecords, out int totalRecords);
                return Ok(new { data= result, filteredRecords, totalRecords});
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
    }
}
