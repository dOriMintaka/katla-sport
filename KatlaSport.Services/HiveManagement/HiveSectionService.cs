﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using KatlaSport.DataAccess;
using KatlaSport.DataAccess.ProductStoreHive;
using DbHiveSection = KatlaSport.DataAccess.ProductStoreHive.StoreHiveSection;

namespace KatlaSport.Services.HiveManagement
{
    /// <summary>
    /// Represents a hive section service.
    /// </summary>
    public class HiveSectionService : IHiveSectionService
    {
        private readonly IProductStoreHiveContext _context;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HiveSectionService"/> class with specified <see cref="IProductStoreHiveContext"/> and <see cref="IUserContext"/>.
        /// </summary>
        /// <param name="context">A <see cref="IProductStoreHiveContext"/>.</param>
        /// <param name="userContext">A <see cref="IUserContext"/>.</param>
        public HiveSectionService(IProductStoreHiveContext context, IUserContext userContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userContext = userContext ?? throw new ArgumentNullException();
        }

        /// <inheritdoc/>
        public async Task<List<HiveSectionListItem>> GetHiveSectionsAsync()
        {
            var dbHiveSections = await _context.Sections.OrderBy(s => s.Id).ToArrayAsync();
            var hiveSections = dbHiveSections.Select(s => Mapper.Map<HiveSectionListItem>(s)).ToList();
            return hiveSections;
        }

        /// <inheritdoc/>
        public async Task<HiveSection> GetHiveSectionAsync(int hiveSectionId)
        {
            var dbHiveSections = await _context.Sections.Where(s => s.Id == hiveSectionId).ToArrayAsync();
            if (dbHiveSections.Length == 0)
            {
                throw new RequestedResourceNotFoundException();
            }

            return Mapper.Map<DbHiveSection, HiveSection>(dbHiveSections[0]);
        }

        /// <inheritdoc/>
        public async Task<List<HiveSectionListItem>> GetHiveSectionsAsync(int hiveId)
        {
            var dbHiveSections = await _context.Sections.Where(s => s.StoreHiveId == hiveId).OrderBy(s => s.Id).ToArrayAsync();
            var hiveSections = dbHiveSections.Select(s => Mapper.Map<HiveSectionListItem>(s)).ToList();
            return hiveSections;
        }

        /// <inheritdoc/>
        public async Task SetStatusAsync(int hiveSectionId, bool deletedStatus)
        {
            var dbSections = await _context.Sections.Where(i => i.Id == hiveSectionId).ToArrayAsync();
            if (dbSections.Length == 0)
            {
                throw new RequestedResourceNotFoundException();
            }

            var dbSection = dbSections[0];
            if (dbSection.IsDeleted != deletedStatus)
            {
                dbSection.IsDeleted = deletedStatus;
                dbSection.LastUpdated = DateTime.UtcNow;
                dbSection.LastUpdatedBy = _userContext.UserId;
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<HiveSection> CreateHiveSectionAsync(UpdateHiveSectionRequest request)
        {
            var dbSections = await _context.Sections.Where(i => i.Code == request.Code).ToArrayAsync();
            if (dbSections.Length > 0)
            {
                throw new RequestedResourceHasConflictException("code");
            }

            var dbHives = await _context.Hives.Where(i => i.Id == request.StoreHiveId).ToArrayAsync();
            if (dbHives.Length == 0)
            {
                throw new RequestedResourceNotFoundException("hive id");
            }

            var dbSection = Mapper.Map<UpdateHiveSectionRequest, DbHiveSection>(request);
            dbSection.CreatedBy = _userContext.UserId;
            dbSection.LastUpdatedBy = _userContext.UserId;
 //           dbSection.StoreHive = dbHives[0];
            _context.Sections.Add(dbSection);
            await _context.SaveChangesAsync();
            return Mapper.Map<HiveSection>(dbSection);
        }

        /// <inheritdoc/>
        public async Task<HiveSection> UpdateHiveSectionAsync(int id, UpdateHiveSectionRequest request)
        {
            var dbSections = await _context.Sections.Where(i => i.Code == request.Code && i.Id != id).ToArrayAsync();
            if (dbSections.Length > 0)
            {
                throw new RequestedResourceHasConflictException("code");
            }

            dbSections = await _context.Sections.Where(i => i.Id == id).ToArrayAsync();
            if (dbSections.Length == 0)
            {
                throw new RequestedResourceNotFoundException();
            }

            var dbHives = await _context.Hives.Where(i => i.Id == request.StoreHiveId).ToArrayAsync();
            if (dbHives.Length == 0)
            {
                throw new RequestedResourceNotFoundException("hive id");
            }

            var dbSection = dbSections[0];
            Mapper.Map(request, dbSection);
            dbSection.LastUpdatedBy = _userContext.UserId;
            await _context.SaveChangesAsync();
            return Mapper.Map<HiveSection>(dbSection);
        }

        /// <inheritdoc/>
        public async Task DeleteHiveSectionAsync(int id)
        {
            var dbSections = await _context.Sections.Where(i => i.Id == id).ToArrayAsync();
            if (dbSections.Length == 0)
            {
                throw new RequestedResourceNotFoundException();
            }

            var dbSection = dbSections[0];
            if (dbSection.IsDeleted == false)
            {
                throw new RequestedResourceHasConflictException();
            }

            _context.Sections.Remove(dbSection);
            await _context.SaveChangesAsync();
        }
    }
}
