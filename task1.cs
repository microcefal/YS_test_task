using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LocationWeb.Server.Data;
using LocationWeb.Shared.Models;

namespace LocationWeb.Server.Controllers
{
    [Route("FileProgects")]
    public class FileProgectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileProgectsController> _logger;

        public FileProgectsController(ApplicationDbContext context, ILogger<FileProgectsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: FileProgects
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var fileProgects = await _context.FileProgect
                .Include(f => f.Project)
                .AsNoTracking()
                .ToListAsync();

            return View(fileProgects);
        }

        // GET: FileProgects/Details/5
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return BadRequest("Invalid ID");

            var fileProgect = await _context.FileProgect
                .Include(f => f.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id.Value);

            if (fileProgect == null)
            {
                _logger.LogWarning("File progect with Id {Id} not found", id);
                return NotFound();
            }

            return View(fileProgect);
        }

        // GET: FileProgects/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            PopulateProjectsDropDown(); 
            return View();
        }

        // POST: FileProgects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProjectId,UrlFile")] FileProgect fileProgect)
        {
            if (!ModelState.IsValid)
            {
                PopulateProjectsDropDown(fileProgect.ProjectId);
                return View(fileProgect);
            }
            try
            {
                _context.Add(fileProgect);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file project");
                ModelState.AddModelError("", "Error creating file project");

                PopulateProjectsDropDown(fileProgect.ProjectId);
                return View(fileProgect);
            }
        }

        // GET: FileProgects/Edit/5
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return BadRequest("Invalid ID");

            var fileProgect = await _context.FileProgect.FindAsync(id.Value);
            if (fileProgect == null)
            {
                _logger.LogWarning("File project with ID {Id} not found for edit", id);
                return NotFound();
            }

            PopulateProjectsDropDown(fileProgect.ProjectId);
            return View(fileProgect);
        }

        // POST: FileProgects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProjectId,UrlFile")] FileProgect fileProgect)
        {
            if (id != fileProgect.Id)
            {
                _logger.LogWarning("ID mismatch in edit: {RouteId} vs {ModelId}",id, fileProgect.Id);
                return BadRequest("ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                PopulateProjectsDropDown(fileProgect.ProjectId);
                return View(fileProgect);
            }

            try
            {
                _context.Update(fileProgect);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await FileProgectExists(fileProgect.Id))
                {
                    _logger.LogWarning("Concurrency exception - file project not found");
                    return NotFound();
                }

                _logger.LogError(ex, "Concurrency error updating file project");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing file project");
                ModelState.AddModelError("", "Error saving changes");

                PopulateProjectsDropDown(fileProgect.ProjectId);
                return View(fileProgect);
            }
        }

        // GET: FileProgects/Delete/5
        [HttpGet("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return BadRequest("Invalid ID");
            
            var fileProgect = await _context.FileProgect
                .Include(f => f.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(fp => fp.Id == id.Value);

            if (fileProgect == null)
            {
                _logger.LogWarning("File project with ID {Id} not found for delete",id);
                return NotFound();
            }
            
            return View(fileProgect);
        }

        // POST: FileProgects/Delete/5
        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fileProject = await _context.FileProgect.FindAsync(id);
            if (fileProject == null)
            {
                _logger.LogWarning("Attempt to delete non-existent file project with ID {Id}", id);
                return NotFound();
            }

            try
            {
                _context.FileProgect.Remove(fileProject);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file project with ID {Id}", id);
                return StatusCode(500, "Error deleting project");
            }
        }

        // POST: FileProgect/UpdateFileUrl/5
        [HttpPost("UpdateFileUrl/{id:int}")]
        public async Task<IActionResult> UpdateFileUrl(int id, [FromBody] UpdateUrlFileDto urlFile)
        {
            if (urlFile == null || string.IsNullOrWhiteSpace(urlFile.UrlFile))
            {
                _logger.LogWarning("Пустой запрос или URL");
                return BadRequest(new
                {
                    success = false,
                    error = "URL файла обязателен"
                });
            }
            if (!Uri.IsWellFormedUriString(urlFile.UrlFile, UriKind.Absolute))
            {
                _logger.LogWarning("Некорректный URL: {Url}", urlFile.UrlFile);
                return BadRequest(new
                {
                    success = false,
                    error = "Неккоретный формат URl"

                });
            }

            var fileProgect = await _context.FileProgect.FindAsync(id);
            if (fileProgect == null)
            {
                _logger.LogWarning("Файл с ID {Id} не найден", id);
                return NotFound(new
                {
                    success = false,
                    error = "Файл с таким ID не найден"
                });
            }
            try
            {
                fileProgect.UrlFile = urlFile.UrlFile;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "URL файла обновлён",
                    fileProgect = new
                    {
                        fileProgect.Id,
                        fileProgect.ProjectId,
                        fileProgect.UrlFile
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении файла {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Ошибка сервера при обновлении URL"
                });
            }
        }

        private async Task<bool> FileProgectExists(int id)
        {
            return await _context.FileProgect.AnyAsync(e => e.Id == id);
        }
        public class UpdateUrlFileDto
        {
            [Required(ErrorMessage = "URL файла обязателен")]
            [Url(ErrorMessage = "Некорректный формат URL")]
            public string UrlFile { get; set; }
        }

        private void PopulateProjectsDropDown(object selectedProject = null)
        {
            ViewData["ProjectId"] = new SelectList(_context.Project, "Id", "Name", selectedProject);
        }
    }
}

