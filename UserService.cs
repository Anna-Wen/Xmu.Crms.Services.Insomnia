using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xmu.Crms.Shared.Exceptions;
using Xmu.Crms.Shared.Models;
using Xmu.Crms.Shared.Service;
using Type = Xmu.Crms.Shared.Models.Type;

namespace Xmu.Crms.Services.Insomnia
{
    public class UserService : IUserService
    {
        private readonly CrmsContext _db;

        public UserService(CrmsContext db) => _db = db;

        public void InsertAttendanceById(long classId, long seminarId, long userId, double longitude, double latitude)
        {
            var usr = GetUserByUserId(userId);
            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            var sem = _db.Seminar.Find(seminarId) ?? throw new SeminarNotFoundException();
            var loc = _db.Location.Include(a => a.ClassInfo).Include(a => a.Seminar)
                          .SingleOrDefault(l => l.Seminar == sem && l.ClassInfo == cls) ??
                      throw new InvalidOperationException();
            _db.Attendences.Add(new Attendance
            {
                AttendanceStatus = loc.Status == 1 ? AttendanceStatus.Present : AttendanceStatus.Late,
                ClassInfo = cls,
                Seminar = sem,
                Student = usr
            });
            _db.SaveChanges();
        }

        public IList<Attendance> ListAttendanceById(long classId, long seminarId)
        {
            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            var sem = _db.Seminar.Find(seminarId) ?? throw new SeminarNotFoundException();
            return _db.Attendences.Include(a => a.ClassInfo).Include(a => a.Seminar)
                .Where(a => a.ClassInfo == cls && a.Seminar == sem).ToList();
        }

        public UserInfo GetUserByUserId(long userId)
        {
            return _db.UserInfo.Include(u => u.School).SingleOrDefault(u => u.Id == userId) ??
                   throw new UserNotFoundException();
        }

        public IList<long> ListUserIdByUserName(string userName)
        {
            return _db.UserInfo.Where(u => u.Name.StartsWith(userName)).Select(u => u.Id).ToList();
        }

        public void UpdateUserByUserId(long userId, UserInfo user)
        {
            var usr = GetUserByUserId(userId);
            usr.Name = user.Name;
            usr.Avatar = user.Avatar;
            usr.Education = user.Education;
            usr.Email = user.Email;
            usr.Gender = user.Gender;
            usr.School = _db.School.Find(user.School.Id);
            usr.Title = user.Title;
            if (usr.Type == Type.Unbinded)
            {
                usr.Type = user.Type;
            }
            else if (usr.Type != user.Type)
            {
                throw new InvalidOperationException();
            }

            _db.SaveChanges();
        }

        public IList<UserInfo> ListUserByClassId(long classId, string numBeginWith, string nameBeginWith)
        {
            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            var userInfos = _db.CourseSelection.Include(c => c.ClassInfo).Include(c => c.Student)
                .Where(c => c.ClassInfo == cls).Select(c => c.Student);
            if (!string.IsNullOrEmpty(nameBeginWith))
            {
                userInfos = userInfos.Where(u => u.Name.StartsWith(nameBeginWith));
            }

            if (!string.IsNullOrEmpty(nameBeginWith))
            {
                userInfos = userInfos.Where(u => u.Number.StartsWith(numBeginWith));
            }

            return userInfos.ToList();
        }

        public IList<UserInfo> ListUserByUserName(string userName)
        {
            return _db.UserInfo.Where(u => u.Name.StartsWith(userName)).ToList();
        }

        public IList<UserInfo> ListPresentStudent(long seminarId, long classId)
        {
            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            var sem = _db.Seminar.Find(seminarId) ?? throw new SeminarNotFoundException();
            return _db.Attendences.Include(a => a.ClassInfo).Include(a => a.Seminar).Where(a =>
                    a.ClassInfo == cls && a.Seminar == sem && a.AttendanceStatus == AttendanceStatus.Present)
                .Select(a => a.Student).ToList();
        }

        public IList<UserInfo> ListLateStudent(long seminarId, long classId)
        {
            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            var sem = _db.Seminar.Find(seminarId) ?? throw new SeminarNotFoundException();
            return _db.Attendences.Include(a => a.ClassInfo).Include(a => a.Seminar).Where(a =>
                    a.ClassInfo == cls && a.Seminar == sem && a.AttendanceStatus == AttendanceStatus.Late)
                .Select(a => a.Student).ToList();
        }

        public IList<UserInfo> ListAbsenceStudent(long seminarId, long classId)
        {
            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            var sem = _db.Seminar.Find(seminarId) ?? throw new SeminarNotFoundException();
            return _db.Attendences.Include(a => a.ClassInfo).Include(a => a.Seminar).Where(a =>
                    a.ClassInfo == cls && a.Seminar == sem && a.AttendanceStatus == AttendanceStatus.Absent)
                .Select(a => a.Student).ToList();
        }

        public IList<Course> ListCourseByTeacherName(string teacherName)
        {
            return _db.Course.Include(c => c.Teacher).Where(c => c.Teacher.Name.StartsWith(teacherName)).ToList();
        }

        public UserInfo GetUserByUserNumber(string userNumber)
        {
            throw new NotImplementedException();
        }
    }
}