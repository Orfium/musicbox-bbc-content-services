using MusicManager.Application.Services;
using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Infrastructure.Repository
{
    public class TagTypeRepository : GenericRepository<tag_type>, ITagTypeRepository
    {
        public TagTypeRepository(MLContext context) : base(context)
        {
            Context = context;
        }

        public MLContext Context { get; }

    }
}
