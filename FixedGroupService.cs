using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xmu.Crms.Shared.Exceptions;
using Xmu.Crms.Shared.Models;
using Xmu.Crms.Shared.Service;

namespace Xmu.Crms.Services.Insomnia
{
    public class FixedGroupService : IFixGroupService
    {
        private readonly CrmsContext _db;

        public FixedGroupService(CrmsContext db) => _db = db;

        /// <summary>
        /// 按班级Id添加固定分组.
        /// @author Group Insomnia
        /// </summary>
        /// <param name="classId">固定分组Id</param>
        /// <param name="userId">队长的Id</param>
        /// <returns>若创建成功返回该条记录的id，失败则返回-1</returns>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.ClassNotFoundException">未找到班级</exception>
        public long InsertFixGroupByClassId(long classId, long userId)
        {
            if (classId <= 0)
            {
                throw new ArgumentException(nameof(classId));
            }

            if (userId <= 0)
            {
                throw new ArgumentException(nameof(userId));
            }

            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            var usr = _db.UserInfo.Find(userId) ?? throw new UserNotFoundException();
            var fg = _db.FixGroup.Add(new FixGroup {ClassInfo = cls, Leader = usr});
            _db.SaveChanges();
            return fg.Entity.Id;
        }

        /// <summary>
        /// 按FixGroupId删除FixGroupMember.
        /// @author Group Insomnia
        /// </summary>
        /// <param name="fixGroupId">固定分组Id</param>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.FixGroupNotFoundException">未找到小组</exception>
        public void DeleteFixGroupMemberByFixGroupId(long fixGroupId)
        {
            if (fixGroupId <= 0)
            {
                throw new ArgumentException(nameof(fixGroupId));
            }

            _db.FixGroupMember.RemoveRange(_db.FixGroupMember.Include(m => m.FixGroup)
                .Where(m => m.FixGroup.Id == fixGroupId));
            _db.SaveChanges();
        }

        public long InsertFixGroupMemberById(long userId, long groupId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(nameof(userId));
            }

            if (groupId <= 0)
            {
                throw new ArgumentException(nameof(groupId));
            }

            var grp = _db.FixGroup.Find(groupId) ?? throw new FixGroupNotFoundException();
            var usr = _db.UserInfo.Find(userId) ?? throw new UserNotFoundException();
            var fgm = _db.FixGroupMember.Add(new FixGroupMember {FixGroup = grp, Student = usr});
            _db.SaveChanges();
            return fgm.Entity.Id;
        }

          /// <summary>
        /// 查询固定小组成员.
        /// @author Group Insomnia
        /// </summary>
        /// <param name="groupId">要查询的固定小组id</param>
        /// <returns>List 固定小组成员信息</returns>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.FixGroupNotFoundException">未找到小组</exception>
        public IList<UserInfo> ListFixGroupMemberByGroupId(long groupId)
        {
            if (groupId <= 0)
            {
                throw new ArgumentException(nameof(groupId));
            }

            var fixGroup = _db.FixGroup.Find(groupId) ?? throw new FixGroupNotFoundException();
            return _db.FixGroupMember.Include(f => f.FixGroup).Include(f => f.Student).Include(u=>u.Student.School)
                .Where(f => f.FixGroupId == groupId).Select(f => f.Student).ToList();
        }

        /// <summary>
        /// 按classId查找FixGroup信息.
        /// @author Group Insomnia
        /// </summary>
        /// <param name="classId">班级Id</param>
        /// <returns>null 固定分组列表</returns>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        public IList<FixGroup> ListFixGroupByClassId(long classId)
        {
            if (classId <= 0)
            {
                throw new ArgumentException(nameof(classId));
            }

            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            return _db.FixGroup.Include(f => f.ClassInfo).Where(f => f.ClassInfo == cls).ToList();
        }

        /// <summary>
        /// 按classId删除FixGroup
        /// @author Group Insomnia
        /// 先根据classId得到所有的FixGroup信息，再根据FixGroupid删除FixGroupMember表的信息，最后再将FixGroup信息删除
        /// </summary>
        /// <param name="classId">班级Id</param>
        /// <seealso cref="M:Xmu.Crms.Shared.Service.IFixGroupService.ListFixGroupByClassId(System.Int64)"/>
        /// <seealso cref="M:Xmu.Crms.Shared.Service.IFixGroupService.DeleteFixGroupByGroupId(System.Int64)"/>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.ClassNotFoundException">未找到班级</exception>
        public void DeleteFixGroupByClassId(long classId)
        {
            if (classId <= 0)
            {
                throw new ArgumentException(nameof(classId));
            }

            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            var members = _db.FixGroupMember.Include(f => f.FixGroup).ThenInclude(f => f.ClassInfo)
                .Where(f => f.FixGroup.ClassInfo == cls);
            var fixGroups = members.Select(m => m.FixGroup).Distinct();
            if(fixGroups!=null)
            {
                _db.FixGroupMember.RemoveRange(members);
                _db.FixGroup.RemoveRange(fixGroups);
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// 删除固定小组.
        /// @author Group Insomnia
        /// </summary>
        /// <param name="groupId">固定小组的id</param>
        /// <seealso cref="M:Xmu.Crms.Shared.Service.IFixGroupService.DeleteFixGroupMemberByFixGroupId(System.Int64)"/>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.FixGroupNotFoundException">未找到小组</exception>
        public void DeleteFixGroupByGroupId(long groupId)
        {
            if (groupId <= 0)
            {
                throw new ArgumentException(nameof(groupId));
            }

            DeleteFixGroupMemberByFixGroupId(groupId);
            _db.Remove(_db.FixGroup.Find(groupId) ?? throw new FixGroupNotFoundException());
        }

        /// <summary>
        /// 修改固定小组.
        /// @author Group Insomnia
        /// 不包括成员
        /// </summary>
        /// <param name="groupId">小组的id</param>
        /// <param name="fixGroupBo">小组信息</param>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.FixGroupNotFoundException">未找到小组</exception>
        public void UpdateFixGroupByGroupId(long groupId, FixGroup fixGroupBo)
        {
            if (groupId <= 0)
            {
                throw new ArgumentException(nameof(groupId));
            }

            var fixGroup = _db.FixGroup.Find(groupId) ?? throw new FixGroupNotFoundException();
            fixGroup.ClassInfo = fixGroupBo.ClassInfo;
            fixGroup.Leader = fixGroupBo.Leader;
            _db.SaveChanges();
        }

        ///<summary>
        ///将学生加入小组
        ///@author Group Insomnia
        ///将用户加入指定的小组
        /// </summary>
        ///  <param name="userId">学生的id</param>
        ///  <param name="groupId">要加入的小组的id</param>
        ///  <returns>long 若创建成功返回该条记录的id，失败则返回-1</returns>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.FixGroupNotFoundException">未找到小组</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.UserNotFoundException">不存在该学生</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.InvalidOperationException">待添加学生已经在小组里了</exception>
        public long InsertStudentIntoGroup(long userId, long groupId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(nameof(userId));
            }

            if (groupId <= 0)
            {
                throw new ArgumentException(nameof(groupId));
            }

            var fixGroup = _db.FixGroup.Find(groupId) ?? throw new FixGroupNotFoundException();
            var entry = _db.FixGroupMember.Add(new FixGroupMember
            {
                FixGroup = fixGroup,
                Student = _db.UserInfo.Find(userId) ?? throw new UserNotFoundException()
            });
            _db.SaveChanges();
            return entry.Entity.Id;
        }

        /// <summary>
        /// 按id获取小组.
        /// @author Group Insomnia
        /// 通过学生id和班级id获取学生所在的班级固定小组
        /// </summary>
        /// <param name="userId">学生id</param>
        /// <param name="classId">班级id</param>
        /// <returns>GroupBO 返回班级固定小组的信息</returns>
        /// <seealso cref="M:Xmu.Crms.Shared.Service.IUserService.GetUserByUserId(System.Int64)"/>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.ClassNotFoundException">未找到班级</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.UserNotFoundException">不存在该学生</exception>
        public FixGroup GetFixedGroupById(long userId, long classId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(nameof(userId));
            }

            if (classId <= 0)
            {
                throw new ArgumentException(nameof(classId));
            }

            var usr = _db.UserInfo.Find(userId) ?? throw new UserNotFoundException();
            var cls = _db.ClassInfo.Find(classId) ?? throw new ClassNotFoundException();
            return _db.FixGroupMember.Include(m => m.Student).ThenInclude(u=>u.School).Include(m => m.FixGroup)
                .ThenInclude(f => f.ClassInfo).Include(u=>u.FixGroup.Leader).ThenInclude(u=>u.School)
                .Where(m => m.Student.Id == userId && m.FixGroup.ClassInfo.Id == classId)
                .Select(m => m.FixGroup).SingleOrDefault();

        }

        ///<summary>
        ///课前将固定小组作为讨论课小组名单
        ///@author Group Insomnia
        ///
        /// </summary>
        /// <param name="seminarId">讨论课Id</param>
        /// <param name="fixedGroupId">小组的Id</param>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.FixGroupNotFoundException">未找到小组</exception>
        public void FixedGroupToSeminarGroup(long semianrId, long fixedGroupId)
        {
            throw new NotImplementedException();
        }

        ///<summary>
        ///按FixGroupId和UserId删除FixGroupMember中某个学生.
        ///@author Group Insomnia
        ///
        /// </summary>
        /// <param name="fixGroupId">固定分组Id</param>
        /// <param name="userId">组员的Id</param>
        /// <exception cref="T:System.ArgumentException">id格式错误</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.FixGroupNotFoundException">未找到小组</exception>
        /// <exception cref="T:Xmu.Crms.Shared.Exceptions.UserNotFoundException">不存在该学生</exception>
        public void DeleteFixGroupUserById(long fixGroupId, long userId)
        {
            var grp = _db.FixGroup.Find(fixGroupId) ?? throw new GroupNotFoundException();
            var usr = _db.UserInfo.Find(userId) ?? throw new UserNotFoundException();
            _db.FixGroupMember.RemoveRange(_db.FixGroupMember.Include(f => f.FixGroup).Where(f => f.FixGroup == grp && f.Student == usr));
            _db.SaveChanges();
        }

        ///<summary>
        ///按照id查询某一固定小组的信息（包括成员）
        ///@author Group Insomnia
        ///
        /// </summary>
        /// <param name="groupId">小组的id</param>
        /// <returns>List 固定小组成员列表对象，若未找到相关成员返回空(null)</returns>
        /// <seealso cref="M:Xmu.Crms.Shared.Service.IFixGroupService.listFixGroupMemberByGroupId(System.Int64)"/>
        public IList<FixGroupMember> ListFixGroupByGroupId(long groupId)
        {
            var grp = _db.FixGroup.Find(groupId) ?? throw new GroupNotFoundException();
            return _db.FixGroupMember.Include(f => f.FixGroup).Where(f => f.FixGroup == grp).ToList();
        }
    }
}