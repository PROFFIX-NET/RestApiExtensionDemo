﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnwesenheitsListeErweiterung.Models;
using AnwesenheitsListeErweiterung.PxRestApi;

namespace AnwesenheitsListeErweiterung
{
    [Route("[controller]")]
    public class AnwesenheitsListeController : Controller
    {
        private readonly IPxRestApiClient HttpClient;

        public AnwesenheitsListeController(IPxRestApiClient httpclient) {
            this.HttpClient = httpclient;
        }

        [HttpGet]
        public async Task<IActionResult> Get() {
            var responseMitarbeiter = await this.HttpClient.GetAsync("pxapi/v2/PRO/Mitarbeiter", "fields=MitarbeiterNr,Name");
            if(responseMitarbeiter.StatusCode != 200) {
                return StatusCode(responseMitarbeiter.StatusCode, responseMitarbeiter.Body);
            }
            var mitarbeiterList = responseMitarbeiter.Body.ToObject<Mitarbeiter[]>();

            var attendanceList = new List<Anwesenheit>();

            foreach (var mitarbeiter in mitarbeiterList) {
                var attendanceListEntry = new Anwesenheit();
                
                // Mitarbeiter Name
                attendanceListEntry.MitarbeiterName = mitarbeiter.Name;

                // Stempelstatus beschaffen
                var responseStempelstatus = await this.HttpClient.GetAsync("pxapi/v2/ZEI/Stempel", $"fields=Eingestempelt,EinstempelnZeitpunkt&mitarbeiter={mitarbeiter.MitarbeiterNr}");
                if (responseStempelstatus.StatusCode != 200) {
                    return StatusCode(responseStempelstatus.StatusCode, responseStempelstatus.Body);
                }
                var stempelstatus = responseStempelstatus.Body.ToObject<Stempel>();

                // Eingestempelt
                attendanceListEntry.Eingestempelt = stempelstatus.Eingestempelt;

                // Dauer berechnen
                if (DateTime.TryParse(stempelstatus.EinstempelnZeitpunkt, out var einstempelZeitpunkt)) {
                    var dauer = DateTime.Now - einstempelZeitpunkt;
                    attendanceListEntry.Dauer = $"{dauer.Hours}h {dauer.Minutes}min {dauer.Seconds}s";
                } else {
                    attendanceListEntry.Dauer = "";
                }

                attendanceList.Add(attendanceListEntry);
            }

            return Ok(attendanceList);
        }
    }
}
