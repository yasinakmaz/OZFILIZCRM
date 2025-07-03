// GlobalUsings.cs - Projenizde gerçekten kullanılan namespace'ler
// Bu dosya projenin root'unda olmalı (CRM.csproj yanında)

// ===============================================
// .NET Core ve System Namespace'leri
// ===============================================
global using System;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Net.Http;
global using System.Net.Http.Json;
global using System.Security.Claims;
global using System.Text;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

// ===============================================
// Microsoft.Extensions (Configuration ve DI)
// ===============================================
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Caching.Memory;

// ===============================================
// MAUI Core Namespace'leri
// ===============================================
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.WebView.Maui;
global using Microsoft.AspNetCore.Components.Forms;
global using Microsoft.AspNetCore.Components.Authorization;
global using Microsoft.AspNetCore.Components.Routing;
global using Microsoft.AspNetCore.Components.Web.Virtualization;
global using Microsoft.Maui;
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Hosting;
global using Microsoft.JSInterop;

// ===============================================
// Entity Framework Core
// ===============================================
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.ChangeTracking;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ===============================================
// Serilog Logging
// ===============================================
global using Serilog;
global using Serilog.Events;

// ===============================================
// Syncfusion UI Controls (Gerçekten Kullanılan)
// ===============================================
global using Syncfusion.Blazor;
global using Syncfusion.Blazor.Grids;
global using Syncfusion.Blazor.Inputs;
global using Syncfusion.Blazor.DropDowns;
global using Syncfusion.Blazor.Buttons;
global using Syncfusion.Blazor.Calendars;
global using Syncfusion.Blazor.Cards;

// ===============================================
// Authentication & Identity
// ===============================================
global using Microsoft.AspNetCore.Authorization;

// ===============================================
// Custom Project Namespace'leri
// ===============================================
global using CRM.Data;
global using CRM.Data.Models;
global using CRM.Data.Repositories;
global using CRM.Data.Migrations;
global using CRM.Services;
global using CRM.DTOs;
global using CRM.Enums;
global using CRM.Components;
global using CRM.Components.Layout;

// ===============================================
// Static Imports (Yararlı olanlar)
// ===============================================
global using static System.String;

// ===============================================
// Type Aliases (Kısayollar)
// ===============================================
global using CancellationToken = System.Threading.CancellationToken;