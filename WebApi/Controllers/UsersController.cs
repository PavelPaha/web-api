using System;
using System.Linq;
using System.Net;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userIdentity = _userRepository.FindById(userId);
            if (userIdentity == null)
                return NotFound();
            var userDto = _mapper.Map<UserDto>(userIdentity);
            return Ok(userDto);
        }

        
        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserInfoDto user)
        {
            if (user == null)
                return BadRequest();

            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError(nameof(UserInfoDto.Login),
                    "Login should contain only letters or digits");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = _mapper.Map<UserEntity>(user);
            var userEntityFromRepository = _userRepository.Insert(userEntity);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntityFromRepository.Id },
                userEntityFromRepository.Id);
        }

        
        [HttpPut("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromBody] UserInfoDto user, [FromRoute] Guid userId)
        {
            if (user == null || userId == Guid.Empty)
                return BadRequest();
            CheckUserForErrors(user);

            var userEntity = new UserEntity(userId);
            _mapper.Map(user, userEntity);
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            _userRepository.UpdateOrInsert(userEntity, out var isInserted);
            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = userEntity.Id },
                    userEntity.Id);
            }
            return NoContent();
        }

        
        private void CheckUserForErrors(UserInfoDto user)
        {
            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError(nameof(UserInfoDto.Login),
                    "Login should contain only letters or digits");

            var userEntity = _mapper.Map<UserEntity>(user);
            if (string.IsNullOrEmpty(userEntity.FirstName))
            {
                ModelState.AddModelError(nameof(UserInfoDto.FirstName),
                    "First name not set");
            }

            if (string.IsNullOrEmpty(userEntity.LastName))
            {
                ModelState.AddModelError(nameof(UserInfoDto.LastName),
                    "Last name not set");
            }
        }
    }
}