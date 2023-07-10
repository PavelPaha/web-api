using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly LinkGenerator _linkGenerator;

        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _linkGenerator = linkGenerator;
        }

        /// <summary>
        /// Получить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "Пользователь не найден")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userIdentity = _userRepository.FindById(userId);
            if (userIdentity == null)
                return NotFound();
            var userDto = _mapper.Map<UserDto>(userIdentity);
            return Ok(userDto);
        }

        
        /// <summary>
        /// Создать пользователя
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        ///
        ///     POST /api/users
        ///     {
        ///        "login": "johndoe375",
        ///        "firstName": "John",
        ///        "lastName": "Doe"
        ///     }
        ///
        /// </remarks>
        /// <param name="user">Данные для создания пользователя</param>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult CreateUser([FromBody] UserInfoDto user)
        {
            if (user == null)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = _mapper.Map<UserEntity>(user);
            var userEntityFromRepository = _userRepository.Insert(userEntity);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntityFromRepository.Id },
                userEntityFromRepository.Id);
        }

        
        /// <summary>
        /// Обновить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="user">Обновленные данные пользователя</param>
        [HttpPut("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
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

        /// <summary>
        /// Частично обновить пользователя
        /// Частично обновить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="patchDoc">JSON Patch для пользователя</param>
        [HttpPatch("{userId}")]
        [Consumes("application/json-patch+json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(404, "Пользователь не найден")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserInfoDto> patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc == null)
                return BadRequest();

            var userFromRepository = _userRepository.FindById(userId);
            if (userFromRepository == null)
                return NotFound();

            var user = _mapper.Map<UserInfoDto>(userFromRepository);
            patchDoc.ApplyTo(user, ModelState);
            TryValidateModel(user);
            CheckUserForErrors(user);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            _mapper.Map(user, userFromRepository);
            _userRepository.Update(userFromRepository);

            return NoContent();
        }
        
        /// <summary>
        /// Удалить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(204, "Пользователь удален")]
        [SwaggerResponse(404, "Пользователь не найден")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userToDeleted = _userRepository.FindById(userId);
            if (userToDeleted == null)
            {
                return NotFound();
            }
            _userRepository.Delete(userId);
            return NoContent();
        }

        /// <summary>
        /// Получить пользователей
        /// </summary>
        /// <param name="pageNumber">Номер страницы, по умолчанию 1</param>
        /// <param name="pageSize">Размер страницы, по умолчанию 20</param>
        /// <response code="200">OK</response>
        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public IActionResult GetUsers(int pageNumber = 1, int pageSize = 10)
        {
            var limitedPageSize = Math.Max(1, Math.Min(pageSize, 20));
            var limitedPageNumber = Math.Max(1, pageNumber);
            
            var page = _userRepository.GetPage(limitedPageNumber, limitedPageSize);
            if (page == null || pageNumber > page.TotalCount)
            {
                return NotFound();
            }
            
            var users = _mapper.Map<IEnumerable<UserDto>>(page);
            var previousPageLink = page.HasPrevious 
                ? GeneratePaginationLinks(limitedPageNumber - 1, limitedPageSize) 
                : null;
            
            var nextPageLink = page.HasNext 
                ? GeneratePaginationLinks(limitedPageNumber + 1, limitedPageSize)
                : null;
            
            var paginationHeader = new
            {
                previousPageLink,
                nextPageLink,
                totalCount = page.TotalCount,
                pageSize = page.PageSize,
                currentPage = page.CurrentPage,
                totalPages = page.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            return Ok(users);
        }

        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult GetUsersOptions()
        {
            Response.Headers.Add("Allow", "POST,GET,OPTIONS");
            return Ok();
        }

        private string GeneratePaginationLinks(int pageNumber, int pageSize)
        {
            return _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
            {
                pageNumber,
                pageSize
            });
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