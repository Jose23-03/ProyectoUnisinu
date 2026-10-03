using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configurar el puerto dinámico de Replit Deployment o usar 8080 por defecto
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configuración de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilitar Swagger SIEMPRE (incluso en producción/despliegue)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Matrículas Unisinú v1");
    c.RoutePrefix = string.Empty; // Carga Swagger directamente en la raíz "/"
});

// ==========================================
// 1. COLECCIONES EN MEMORIA (Simulación BD)
// ==========================================

var estudiantes = new List
{
    new Estudiante(1, "José Sepúlveda", "Ingeniería de Sistemas"),
    new Estudiante(2, "María Pérez", "Ingeniería de Sistemas"),
    new Estudiante(3, "Carlos Gómez", "Ingeniería Industrial")
};

var asignaturas = new List
{
    new Asignatura(101, "Estructuras de Datos", "SIS-101", 3),
    new Asignatura(102, "Programación Web", "SIS-102", 4),
    new Asignatura(103, "Bases de Datos I", "SIS-103", 3)
};

var matriculas = new List();

// ==========================================
// 2. ENDPOINTS (Minimal API)
// ==========================================

// GET /api/asignaturas -> Consultar únicamente asignaturas activas[cite: 2]
app.MapGet("/api/asignaturas", () =>
{
    var activas = asignaturas.Where(a => a.Activa).ToList();
    return Results.Ok(activas);
});

// POST /api/asignaturas -> Registrar una nueva asignatura[cite: 2]
app.MapPost("/api/asignaturas", (Asignatura nuevaAsignatura) =>
{
    if (asignaturas.Any(a => a.Id == nuevaAsignatura.Id || a.Codigo == nuevaAsignatura.Codigo))
    {
        return Results.BadRequest(new { Mensaje = "Ya existe una asignatura con el mismo Id o Código." });
    }

    asignaturas.Add(nuevaAsignatura);
    return Results.Created($"/api/asignaturas/{nuevaAsignatura.Id}", nuevaAsignatura);
});

// DELETE /api/asignaturas/{id} -> Borrado Físico o Borrado Lógico según historial[cite: 2]
app.MapDelete("/api/asignaturas/{id:int}", (int id) =>
{
    var asignatura = asignaturas.FirstOrDefault(a => a.Id == id);
    if (asignatura == null)
    {
        return Results.NotFound(new { Mensaje = $"La asignatura con Id {id} no existe." });
    }

    bool tieneMatriculas = matriculas.Any(m => m.AsignaturaId == id);

    if (!tieneMatriculas)
    {
        asignaturas.Remove(asignatura);
        return Results.Ok(new { Mensaje = $"Asignatura {id} eliminada físicamente de la lista." });
    }
    else
    {
        int index = asignaturas.IndexOf(asignatura);
        asignaturas[index] = asignatura with { Activa = false };
        return Results.Ok(new { Mensaje = $"Asignatura {id} tiene historial de matrículas. Se ha inactivado (Borrado Lógico)." });
    }
});

// POST /api/matriculas -> Registrar matrícula validando reglas de negocio[cite: 2]
app.MapPost("/api/matriculas", (MatriculaDTO dto) =>
{
    var estudiante = estudiantes.FirstOrDefault(e => e.Id == dto.EstudianteId);
    if (estudiante == null)
    {
        return Results.NotFound(new { Mensaje = $"El estudiante con Id {dto.EstudianteId} no existe." });
    }

    var asignatura = asignaturas.FirstOrDefault(a => a.Id == dto.AsignaturaId);
    if (asignatura == null)
    {
        return Results.NotFound(new { Mensaje = $"La asignatura con Id {dto.AsignaturaId} no existe." });
    }

    if (!asignatura.Activa)
    {
        return Results.BadRequest(new { Mensaje = $"La asignatura '{asignatura.Nombre}' está inactiva y no admite nuevas matrículas." });
    }

    bool yaMatriculado = matriculas.Any(m =>
        m.EstudianteId == dto.EstudianteId &&
        m.AsignaturaId == dto.AsignaturaId &&
        m.Anio == dto.Anio &&
        m.Periodo == dto.Periodo);

    if (yaMatriculado)
    {
        return Results.BadRequest(new { Mensaje = $"El estudiante ya se encuentra matriculado en esta asignatura para el periodo {dto.Anio}-{dto.Periodo}." });
    }

    int nuevoId = matriculas.Count > 0 ? matriculas.Max(m => m.Id) + 1 : 1;
    var nuevaMatricula = new Matricula(nuevoId, dto.EstudianteId, dto.AsignaturaId, dto.Anio, dto.Periodo);
    matriculas.Add(nuevaMatricula);

    return Results.Created($"/api/matriculas/{nuevaMatricula.Id}", nuevaMatricula);
});

// GET /api/estudiantes/{id}/asignaturas -> Consultar historial del estudiante[cite: 2]
app.MapGet("/api/estudiantes/{id:int}/asignaturas", (int id) =>
{
    var estudiante = estudiantes.FirstOrDefault(e => e.Id == id);
    if (estudiante == null)
    {
        return Results.NotFound(new { Mensaje = $"El estudiante con Id {id} no existe." });
    }

    var historial = matriculas
        .Where(m => m.EstudianteId == id)
        .Select(m =>
        {
            var asig = asignaturas.FirstOrDefault(a => a.Id == m.AsignaturaId);
            return new
            {
                MatriculaId = m.Id,
                AsignaturaId = m.AsignaturaId,
                NombreAsignatura = asig?.Nombre ?? "Desconocida",
                Codigo = asig?.Codigo,
                Creditos = asig?.Creditos,
                Anio = m.Anio,
                Periodo = m.Periodo
            };
        })
        .ToList();

    return Results.Ok(new
    {
        Estudiante = estudiante.Nombre,
        Carrera = estudiante.Carrera,
        TotalAsignaturas = historial.Count,
        Asignaturas = historial
    });
});

app.Run();

// ==========================================
// 3. MODELOS Y DTOS[cite: 1]
// ==========================================

public record Estudiante(int Id, string Nombre, string Carrera);
public record Asignatura(int Id, string Nombre, string Codigo, int Creditos, bool Activa = true);
public record Matricula(int Id, int EstudianteId, int AsignaturaId, int Anio, string Periodo);
public record MatriculaDTO(int EstudianteId, int AsignaturaId, int Anio, string Periodo);