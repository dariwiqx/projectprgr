using System;
using System.Collections.Generic;
using прпгр.Models;

namespace прпгр.Services
{
    public class LmsCourse
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class LmsService
    {
        // Mock implementation simulating Moodle/Blackboard API

        public bool VerifyConnection(string url, string token, string type)
        {
            // Simulate verification - accept any non-empty values
            return !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(token);
        }

        public List<LmsCourse> GetCourses(LMSAccountLink link)
        {
            // Return mock courses based on LMS type
            if (link.LmsType == "Moodle")
            {
                return new List<LmsCourse>
                {
                    new() { Id = "moodle-101", Name = "Математический анализ", Description = "Курс по матанализу, 1 семестр" },
                    new() { Id = "moodle-102", Name = "Линейная алгебра", Description = "Курс по линейной алгебре" },
                    new() { Id = "moodle-103", Name = "Физика", Description = "Общая физика, механика" },
                    new() { Id = "moodle-201", Name = "Программирование на C#", Description = "Основы ООП на C#" },
                    new() { Id = "moodle-202", Name = "Базы данных", Description = "Реляционные БД и SQL" }
                };
            }
            else
            {
                return new List<LmsCourse>
                {
                    new() { Id = "bb-001", Name = "Дискретная математика", Description = "Теория множеств и графов" },
                    new() { Id = "bb-002", Name = "Алгоритмы и структуры данных", Description = "Сортировки, деревья, графы" },
                    new() { Id = "bb-003", Name = "Операционные системы", Description = "Процессы, потоки, память" },
                    new() { Id = "bb-004", Name = "Компьютерные сети", Description = "TCP/IP, маршрутизация" }
                };
            }
        }

        public (bool Success, string Message) PublishMaterial(LMSAccountLink link, string courseId, Material material)
        {
            // Simulate publishing
            if (string.IsNullOrWhiteSpace(courseId))
                return (false, "Не выбран курс для публикации.");

            if (material == null)
                return (false, "Материал не найден.");

            // Simulate a successful publish
            return (true, $"Материал \"{material.Title}\" успешно опубликован в курсе {courseId} ({link.LmsType}).");
        }
    }
}
