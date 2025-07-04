﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Services.Enum;
using Services.Languages;
using Services.Localizations;
using Web.Infrastructure;
using Web.Models;
using Web.Models.Localizations;
using X.PagedList;

namespace Web.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class LocalizationController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly ILocalizationsService _localizationsService;
        private readonly ILanguageService _languageService;

        public LocalizationController(IMapper mapper, ILocalizationsService localizationsService, ILanguageService languageService)
        {
            _mapper = mapper;
            _localizationsService = localizationsService;
            _languageService = languageService;
        }

        [HttpGet]
     
        public IActionResult LocalizationAdmin(int? page = 1, string searchName = null, int LanguageId = 0, string keyvalue = null, string resourcekey = null, IEnumerable<int> SearchLanguages = null)
        {
            var model = new IndexLocalizationViewModel();
            var pagenumber = page ?? 1;
            var pageSize = 20;
            var dbResult = _localizationsService.GetAllAsQuerable(searchName, keyvalue, resourcekey, SearchLanguages).ToPagedList(pagenumber, pageSize);
            model.localizations = _mapper.Map<List<LocalizationViewModel>>(dbResult);
            model.listlanguages = _mapper.Map<List<LanguageViewModel>>(_languageService.GetAllActive());

            //ViewBag.searchName = searchName;
            //ViewBag.searchLang = searchLang;
            //ViewBag.LanguageId = LanguageId;
            //ViewBag.keyvalue = keyvalue;
            //ViewBag.resourcekey = resourcekey;
            //ViewBag.Localization = dbResult;
            model.SearchLanguages = SearchLanguages;
            return View(model);
        }
        [HttpGet]
        [ClaimRequirement(Mode.CanEit, ControllerEnum.Localization)]
        public IActionResult Force2Reset(string lang)
        {
            ClearCachedResource();
            var result = GetCachedResource(lang);
            ViewData["Shared"] = result.ToDictionary(x => x.KeyName, x => x.KeyValue);

            return RedirectToAction("LocalizationAdmin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClaimRequirement(Mode.CanEit, ControllerEnum.Localization)]
        public JsonResult AddUpdateLocalization(CreateUpdateLocalizationViewModel model)
        {
            var sharedRes = (Dictionary<string, string>)ViewData["Shared"];

            if (model.Id != 0 && String.IsNullOrEmpty(model.KeyValue))
            {
                return Json(new { success = false, msg = sharedRes["FillRequiredFields"] });
            }
            if (model.Id == 0)
            {
                if (String.IsNullOrEmpty(model.KeyName) || String.IsNullOrEmpty(model.KeyValue) )
                {
                    return Json(new { success = false, msg = sharedRes["FillRequiredFields"] });
                }
            }

            if (model.Id != 0)
            {
                var models = _mapper.Map<UpdateLocalizationViewModel>(model);
                var dbLocalization = _localizationsService.GetById(model.Id);
                dbLocalization = _mapper.Map(models, dbLocalization);
                _localizationsService.Update(dbLocalization);
                _localizationsService.SaveChanges();
            }
            else
            { 
                var checkKeyExists = _localizationsService.CheckByKeyName(model.KeyName);
                if (checkKeyExists)
                { return Json(new { success = false, msg = "this KeyName already exists!" }); }

                var lang = _mapper.Map<List<LanguageViewModel>>(_languageService.GetAll());
                List<Localization> localizatonsModel = new List<Localization>();

               

                foreach (var item in lang)
                {
                    var mappedLocalization = _mapper.Map<Localization>(model);
                    if (item.Id == 1)
                    {
                        mappedLocalization.LanguageId =1;
                        localizatonsModel.Add(mappedLocalization);
                    }
                    if (item.Id == 2)
                    {
                         
                        mappedLocalization.LanguageId = item.Id;
                        mappedLocalization.KeyValue = model.KeyValueEn ;  
                        localizatonsModel.Add(mappedLocalization);
                    }
                    if (item.Id == 3)
                    {
                         
                        mappedLocalization.LanguageId = item.Id;
                        mappedLocalization.KeyValue = model.KeyValueSr; 
                        localizatonsModel.Add(mappedLocalization);
                    }
                }

                _localizationsService.InsertRange(localizatonsModel);
                _localizationsService.SaveChanges();
            }
            return Json(new { success = true });
        }




        [AllowAnonymous]
        [HttpGet]
        public IActionResult AddLocalization()
        {
            return View();
        }


        [AllowAnonymous]
        [HttpPost]
        public JsonResult AddLocalizationIfNotExists(IEnumerable<string> keyforAdd)
        {
            //TODO: Duhet me i validu nese egziston i njejti ne database

            var lang = _mapper.Map<List<LanguageViewModel>>(_languageService.GetAllActive());
            List<Localization> localizatonsModel = new List<Localization>();

            IEnumerable<string> keyforAddAsDistinct = keyforAdd.Select(x => x).Distinct();

            foreach (var key in keyforAddAsDistinct)
            {
                foreach (var item in lang)
                {
                    var mappedLocalization = new Localization();
                    mappedLocalization.LanguageId = item.Id;
                    mappedLocalization.KeyName = key;
                    mappedLocalization.KeyValue = key + "_" + item.Code;
                    mappedLocalization.ResourceKey = "Shared";
                    localizatonsModel.Add(mappedLocalization);

                }

            }
            _localizationsService.InsertRange(localizatonsModel);
            _localizationsService.SaveChanges();
            ClearCachedResource();
            return Json(new { success = true });
        }
    }
}