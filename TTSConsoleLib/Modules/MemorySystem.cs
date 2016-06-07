﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using ConsoleStore.Models;
using ConsoleStore.Context;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace TTSConsoleLib.Modules
{
    /// <summary>
    /// Sqlite Database to store User and Poll Data.
    /// </summary>
    public class MemorySystem
    {
        #region Singleton
        static MemorySystem()
        {
            __instance = new MemorySystem();
        }
        private static MemorySystem __instance;
        public static MemorySystem _instance
        {
            get
            {
                return __instance;
            }
        }
        //Not Constructable
        private MemorySystem()
        { }
        #endregion

        public void Init()
        {
            using (var db = new ConsoleContext())
            {
                db.Database.EnsureCreated();
                db.Database.Migrate();
            }
        }

        public void UserPointPlusPlus(String pUserName)
        {
            using (var db = new ConsoleContext())
            {
                var user = db.tblUser.Include(i=>i.Points).FirstOrDefault(x => x.Name == pUserName);
                if (user == null)
                {
                    //Make
                    user = new User() { Name = pUserName };
                    db.tblUser.Add(user);
                    db.SaveChanges();
                    user = db.tblUser.Include(i => i.Points).FirstOrDefault(x => x.Name == pUserName);
                }

                var points = user.Points.FirstOrDefault(x => x.Date == DateTime.Now.Date);
                if (points == null)
                {
                    //Make
                    var po = new Point() { Date = DateTime.Now.Date, Count = 1, User = user }; 
                    db.tblPoint.Add(po);
                    db.SaveChanges();

                    user.Points.Add(po);
                }
                else
                {
                    //Modify
                    points.Count++;
                }

                db.SaveChanges();
            }
        }

        public String GetUserPoints()
        {
            using (var db = new ConsoleContext())
            {
                var pointsquery = db.tblUser.Select(
                        x => new
                        {
                            x.Name,
                            points = x.Points.Select(s => s.Count).Sum()
                        }
                    );
                var points = pointsquery.ToList();

                StringBuilder sb = new StringBuilder();
                foreach (var result in points)
                {
                    sb.Append((result?.Name ?? "N/A") + " Has " + (result?.points.ToString() ?? "-1") + " Points! -- ");
                }

                return sb.ToString();
            }
        }

        public DateTime UsersLastActiveDate(String pUserName)
        {
            using (var db = new ConsoleContext())
            {
                return db.tblPoint.Where(x => x.User.Name == pUserName)?.Select(s => s.Date)?.Max() ?? DateTime.MinValue;
            }
        }

        
        public bool NewPoll(String pName, String[] pOptions, DateTime pDuration)
        {
            // New Poll!!
            return true;
        }
        public String EndPoll(String pName)
        {
            return String.Empty;
        }
        public String[] CurrentPolls()
        {
            return new String[] { };
        }
        public bool AddUserToPollWithOption(String pName, String pUserName, String pPollOption)
        {
            return true;
        }


    }
}

namespace ConsoleStore.Context
{
    public class ConsoleContext : DbContext
    {

        // This property defines the table
        public DbSet<Channel> tblChannel { get; set; }
        public DbSet<User> tblUser { get; set; }
        public DbSet<Point> tblPoint { get; set; }
        public DbSet<Poll> tblPoll { get; set; }
        public DbSet<PollOption> tblPollOption { get; set; }

        // This method connects the context with the database
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "ConsoleTTS.db");
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = path };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);

            optionsBuilder.UseSqlite(connection);
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Point>()
            //    .HasOne(p => p.User)
            //    .WithMany(b => b.Points)
            //    .HasForeignKey(f=>f.FK_User);

            //modelBuilder.Entity<User>()
            //    .HasMany(b => b.Points)
            //    .WithOne();
        }
    }
}

namespace ConsoleStore.Models
{
    public class BaseTable
    {
        [Key]
        public virtual int Id { get; set; }
        public virtual String Name { get; set; }
    }

    public class Channel : BaseTable
    {
        public virtual List<User> Users { get; set; }
    }

    /// <summary>
    ///  Channel Viewers
    /// </summary>
    public class User : BaseTable
    {
        public String Voice { get; set; }
        public String Lexicon { get; set; }

        public virtual List<Point> Points { get; set; }
    }

    /// <summary>
    /// Channel Points
    /// </summary>
    public class Point : BaseTable
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }


        //public int FK_User { get; set; }
        //[ForeignKey("FK_User")]
        public virtual User User { get; set; }
    }


    /// <summary>
    /// Channel Polls
    /// </summary>
    public class Poll : BaseTable
    {
        public String PollName { get; set; }
        public String Commands { get; set; }
        public DateTime EndDateTime { get; set; }
        public bool Active { get; set; }

        [NotMapped]
        public List<String> EnteredUsers
        {
            get
            {
                return PollOptions?.Select(s => s.User.Name).ToList();
            }
        }


        public virtual List<PollOption> PollOptions { get; set; }
    }

    /// <summary>
    /// Channel Poll Selection by Users
    /// </summary>
    public class PollOption : BaseTable
    {
        public virtual Poll Poll { get; set; }
        public virtual User User { get; set; }
    }

}