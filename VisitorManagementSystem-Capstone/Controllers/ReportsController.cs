// ReportsController.cs - Fixed and Updated
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Models;
using VisitorManagementSystem_Capstone.ViewModels;

namespace VisitorManagementSystem_Capstone.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly VisitorManagementSystemDatabaseContext _context;
        private readonly POSTViewModel _postViewModel;

        public ReportsController(VisitorManagementSystemDatabaseContext context, POSTViewModel postViewModel)
        {
            _context = context;
            _postViewModel = postViewModel;
        }

        // GET: Reports
        public IActionResult VisitorReports()
        {
            return View();
        }

        // GET: Visitor Reports Data
        [HttpGet]
        public async Task<IActionResult> GetVisitorReports(DateTime? startDate, DateTime? endDate, string reportType = "daily")
        {
            try
            {
                // Set default date range if not provided
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Today;
                    startDate = reportType.ToLower() switch
                    {
                        "weekly" => endDate.Value.AddDays(-7),
                        "monthly" => endDate.Value.AddMonths(-1),
                        "yearly" => endDate.Value.AddYears(-1),
                        _ => endDate.Value // daily
                    };
                }

                // Ensure start date is before end date
                if (startDate > endDate)
                {
                    (startDate, endDate) = (endDate, startDate);
                }

                var startDateOnly = DateOnly.FromDateTime(startDate.Value);
                var endDateOnly = DateOnly.FromDateTime(endDate.Value);

                // Get visit logs with related data
                var visitLogs = await _context.VisitLogs
                    .Include(v => v.Visitor)
                        .ThenInclude(v => v.User)
                    .Include(v => v.RoomOwner)
                        .ThenInclude(r => r.User)
                    .Include(v => v.Room)
                    .Include(v => v.CheckInOut)
                    .Where(v => v.IssueDate >= startDateOnly && v.IssueDate <= endDateOnly && v.logStatus == true)
                    .OrderByDescending(v => v.IssueDate)
                    .ThenByDescending(v => v.VisitLogId)
                    .ToListAsync();

                // Generate report data using POSTViewModel
                var reportData = await GenerateVisitorReportAsync(
                    visitLogs, startDate.Value, endDate.Value, reportType);

                return Json(new
                {
                    success = true,
                    data = reportData,
                    totalRecords = visitLogs.Count,
                    dateRange = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error generating report: {ex.Message}"
                });
            }
        }

        // GET: Export Visitor Reports
        [HttpGet]
        public async Task<IActionResult> ExportVisitorReports(DateTime? startDate, DateTime? endDate, string reportType = "daily", string format = "excel")
        {
            try
            {
                // Set default date range if not provided
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Today;
                    startDate = reportType.ToLower() switch
                    {
                        "weekly" => endDate.Value.AddDays(-7),
                        "monthly" => endDate.Value.AddMonths(-1),
                        "yearly" => endDate.Value.AddYears(-1),
                        _ => endDate.Value // daily
                    };
                }

                var startDateOnly = DateOnly.FromDateTime(startDate.Value);
                var endDateOnly = DateOnly.FromDateTime(endDate.Value);

                // Get visit logs with related data
                var visitLogs = await _context.VisitLogs
                    .Include(v => v.Visitor)
                        .ThenInclude(v => v.User)
                    .Include(v => v.RoomOwner)
                        .ThenInclude(r => r.User)
                    .Include(v => v.Room)
                    .Include(v => v.CheckInOut)
                    .Where(v => v.IssueDate >= startDateOnly && v.IssueDate <= endDateOnly && v.logStatus == true)
                    .OrderByDescending(v => v.IssueDate)
                    .ThenByDescending(v => v.VisitLogId)
                    .ToListAsync();

                // Generate report data using POSTViewModel
                var reportData = await GenerateVisitorReportAsync(
                    visitLogs, startDate.Value, endDate.Value, reportType);

                if (format.ToLower() == "excel")
                {
                    return ExportToExcel(reportData, startDate.Value, endDate.Value, reportType);
                }
                else
                {
                    return ExportToPDF(reportData, startDate.Value, endDate.Value, reportType);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error exporting report: {ex.Message}";
                return RedirectToAction(nameof(VisitorReports));
            }
        }

        private IActionResult ExportToExcel(List<VisitorReportItem> reportData, DateTime startDate, DateTime endDate, string reportType)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Visitor Report");
                var currentRow = 1;

                // Title
                worksheet.Cell(currentRow, 1).Value = $"Visitor Management System - {reportType.ToUpper()} Report";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
                worksheet.Range(currentRow, 1, currentRow, 8).Merge();

                currentRow++;
                worksheet.Cell(currentRow, 1).Value = $"Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
                worksheet.Range(currentRow, 1, currentRow, 8).Merge();
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = $"Generated On: {DateTime.Now:yyyy-MM-dd HH:mm}";
                worksheet.Range(currentRow, 1, currentRow, 8).Merge();
                currentRow++;
                currentRow++; // Empty row

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Date";
                worksheet.Cell(currentRow, 2).Value = "Visitor Name";
                worksheet.Cell(currentRow, 3).Value = "Contact";
                worksheet.Cell(currentRow, 4).Value = "Room";
                worksheet.Cell(currentRow, 5).Value = "Room Owner";
                worksheet.Cell(currentRow, 6).Value = "Purpose";
                worksheet.Cell(currentRow, 7).Value = "Status";
                worksheet.Cell(currentRow, 8).Value = "Check-in Time";

                // Style headers
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentRow++;

                // Data
                foreach (var item in reportData)
                {
                    worksheet.Cell(currentRow, 1).Value = item.Date;
                    worksheet.Cell(currentRow, 2).Value = item.VisitorName;
                    worksheet.Cell(currentRow, 3).Value = item.ContactNumber;
                    worksheet.Cell(currentRow, 4).Value = item.RoomNumber;
                    worksheet.Cell(currentRow, 5).Value = item.RoomOwnerName;
                    worksheet.Cell(currentRow, 6).Value = item.Purpose;
                    worksheet.Cell(currentRow, 7).Value = item.Status;
                    worksheet.Cell(currentRow, 8).Value = item.CheckInTime;

                    currentRow++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"VisitorReport_{reportType}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx"
                    );
                }
            }
        }

        private IActionResult ExportToPDF(List<VisitorReportItem> reportData, DateTime startDate, DateTime endDate, string reportType)
        {
            // This would be implemented with a PDF library like iTextSharp or QuestPDF
            // For now, we'll redirect to Excel export
            return ExportToExcel(reportData, startDate, endDate, reportType);
        }

        // GET: Dashboard Statistics
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);

                var totalVisitorsToday = await _context.VisitLogs
                    .CountAsync(v => v.IssueDate == today && v.logStatus == true);

                var checkedInVisitors = await _context.VisitLogs
                    .Include(v => v.CheckInOut)
                    .CountAsync(v => v.IssueDate == today &&
                                   v.logStatus == true &&
                                   v.CheckInOut != null &&
                                   v.CheckInOut.CheckOutDateTime == null);

                var pendingApprovals = await _context.VisitLogs
                    .CountAsync(v => v.IssueDate == today &&
                                   v.logStatus == true &&
                                   v.VerificationStatus == null);

                var totalVisitorsThisMonth = await _context.VisitLogs
                    .CountAsync(v => v.IssueDate.Month == DateTime.Today.Month &&
                                   v.IssueDate.Year == DateTime.Today.Year &&
                                   v.logStatus == true);

                return Json(new
                {
                    success = true,
                    totalVisitorsToday,
                    checkedInVisitors,
                    pendingApprovals,
                    totalVisitorsThisMonth
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error fetching statistics: {ex.Message}"
                });
            }
        }

        // GET: Visitor Trends Data
        [HttpGet]
        public async Task<IActionResult> GetVisitorTrends(string period = "monthly")
        {
            try
            {
                var endDate = DateTime.Today;
                var startDate = period.ToLower() switch
                {
                    "weekly" => endDate.AddDays(-30), // 30 days for better trend visualization
                    "yearly" => endDate.AddYears(-1),
                    _ => endDate.AddMonths(-6) // default to 6 months
                };

                var trendsData = await GenerateVisitorTrendsAsync(_context, startDate, endDate, period);

                return Json(new
                {
                    success = true,
                    data = trendsData,
                    period = period
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error generating trends: {ex.Message}"
                });
            }
        }
    public async Task<List<VisitorReportItem>> GenerateVisitorReportAsync(
        List<VisitLog> visitLogs, DateTime startDate, DateTime endDate, string reportType)
            {
                var reportItems = new List<VisitorReportItem>();

                foreach (var log in visitLogs)
                {
                    var status = log.VerificationStatus switch
                    {
                        true => "Approved",
                        false => "Declined",
                        null => "Pending"
                    };

                    if (log.CheckInOut != null && log.CheckInOut.CheckOutDateTime != null)
                    {
                        status = "Checked Out";
                    }
                    else if (log.CheckInOut != null)
                    {
                        status = "Checked In";
                    }

                    reportItems.Add(new VisitorReportItem
                    {
                        Date = log.IssueDate.ToString("yyyy-MM-dd"),
                        VisitorName = $"{log.Visitor?.User?.FirstName} {log.Visitor?.User?.LastName}",
                        ContactNumber = log.Visitor?.User?.ContactNumber,
                        RoomNumber = log.Room?.RoomNumber,
                        RoomOwnerName = $"{log.RoomOwner?.User?.FirstName} {log.RoomOwner?.User?.LastName}",
                        Purpose = log.PurposeOfVisit,
                        Status = status,
                        CheckInTime = log.CheckInOut?.CheckInDateTime.ToString("HH:mm")
                    });
                }

                return reportItems;
            }

            public async Task<List<VisitorTrendItem>> GenerateVisitorTrendsAsync(
                VisitorManagementSystemDatabaseContext context, DateTime startDate, DateTime endDate, string period)
            {
                var trends = new List<VisitorTrendItem>();
                var visitLogs = await context.VisitLogs
                    .Include(v => v.Visitor)
                        .ThenInclude(v => v.User)
                    .Include(v => v.RoomOwner)
                        .ThenInclude(r => r.User)
                    .Include(v => v.Room)
                    .Include(v => v.CheckInOut)
                    .Where(v => v.IssueDate >= DateOnly.FromDateTime(startDate) &&
                               v.IssueDate <= DateOnly.FromDateTime(endDate) &&
                               v.logStatus == true)
                    .ToListAsync();

                // Group by period
                var groupedData = period.ToLower() switch
                {
                    "weekly" => visitLogs.GroupBy(v => $"{v.IssueDate:yyyy}-W{GetWeekOfYear(v.IssueDate.ToDateTime(TimeOnly.MinValue))}"),
                    "yearly" => visitLogs.GroupBy(v => $"{v.IssueDate:yyyy}"),
                    _ => visitLogs.GroupBy(v => $"{v.IssueDate:yyyy-MM}") // monthly by default
                };

                foreach (var group in groupedData.OrderBy(g => g.Key))
                {
                    trends.Add(new VisitorTrendItem
                    {
                        Period = group.Key,
                        TotalVisitors = group.Count(),
                        Approved = group.Count(v => v.VerificationStatus == true),
                        Pending = group.Count(v => v.VerificationStatus == null),
                        Declined = group.Count(v => v.VerificationStatus == false)
                    });
                }

                return trends;
            }

            private int GetWeekOfYear(DateTime date)
            {
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                return culture.Calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
            }
        }
}

