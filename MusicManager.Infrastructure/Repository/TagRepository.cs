using MusicManager.Application.Services;
using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Infrastructure.Repository
{
    public class TagRepository : GenericRepository<tag>, ITagRepository
    {
        public TagRepository(MLContext context) : base(context)
        {
            Context = context;
        }

        public MLContext Context { get; }
        
    }
}
