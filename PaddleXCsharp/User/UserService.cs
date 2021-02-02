﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Learun.DataBase.Repository;
using Learun.Util;
using Learun.Cache.Base;
using Learun.Cache.Factory;

namespace PaddleXCsharp.User
{
    public class UserService : RepositoryFactory
    {
        #region 缓存定义
        private ICache cache = CacheFactory.CaChe();
        private string cacheKey = "learun_adms_user_";       // +公司主键
        private string cacheKeyAccount = "learun_adms_user_account_";// +用户账号（账号不允许改动）
        private string cacheKeyId = "learun_adms_user_Id_";// +用户账号（账号不允许改动）
        #endregion

        #region 属性 构造函数
        private string fieldSql;
        public UserService()
        {
            fieldSql = @" 
                        t.F_UserId,
                        t.F_EnCode,
                        t.F_Account,
                        t.F_Password,
                        t.F_Secretkey,
                        t.F_RealName,
                        t.F_NickName,
                        t.F_HeadIcon,
                        t.F_QuickQuery,
                        t.F_SimpleSpelling,
                        t.F_Gender,
                        t.F_Birthday,
                        t.F_Mobile,
                        t.F_Telephone,
                        t.F_Email,
                        t.F_OICQ,
                        t.F_WeChat,
                        t.F_MSN,
                        t.F_CompanyId,
                        t.F_DepartmentId,
                        t.F_SecurityLevel,
                        t.F_OpenId,
                        t.F_Question,
                        t.F_AnswerQuestion,
                        t.F_CheckOnLine,
                        t.F_AllowStartTime,
                        t.F_AllowEndTime,
                        t.F_LockStartDate,
                        t.F_LockEndDate,
                        t.F_SortCode,
                        t.F_DeleteMark,
                        t.F_EnabledMark,
                        t.F_Description,
                        t.F_CreateDate,
                        t.F_CreateUserId,
                        t.F_CreateUserName,
                        t.F_ModifyDate,
                        t.F_ModifyUserId,
                        t.F_ModifyUserName
                        ";
        }
        #endregion

        #region 获取数据
        /// <summary>
        /// 获取实体,通过用户账号
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns></returns>
        public UserEntity GetEntityByAccount(string account)
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("SELECT ");
                strSql.Append(fieldSql);
                strSql.Append(" FROM LR_Base_User t ");
                strSql.Append(" WHERE t.F_Account = @account AND t.F_DeleteMark = 0  ");
                return this.BaseRepository().FindEntity<UserEntity>(strSql.ToString(), new { account = account });
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        /// <summary>
        /// 用户列表(根据公司主键)
        /// </summary>
        /// <param name="companyId">公司主键</param>
        /// <returns></returns>
        public IEnumerable<UserEntity> GetList(string companyId)
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("SELECT ");
                strSql.Append(fieldSql.Replace("t.F_Password,", "").Replace("t.F_Secretkey,", ""));
                strSql.Append(" FROM LR_Base_User t WHERE t.F_DeleteMark = 0 AND t.F_CompanyId = @companyId ORDER BY t.F_DepartmentId,t.F_RealName ");
                return this.BaseRepository().FindList<UserEntity>(strSql.ToString(), new { companyId = companyId });
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }


        /// <summary>
        /// 用户列表(根据公司主键)(分页)
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="departmentId"></param>
        /// <param name="pagination"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public IEnumerable<UserEntity> GetPageList(string companyId, string departmentId, Pagination pagination, string keyword)
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("SELECT ");
                strSql.Append(fieldSql.Replace("t.F_Password,", "").Replace("t.F_Secretkey,", ""));
                strSql.Append(" FROM LR_Base_User t WHERE t.F_DeleteMark = 0 AND t.F_CompanyId = @companyId  ");

                if (!string.IsNullOrEmpty(departmentId))
                {
                    strSql.Append(" AND t.F_DepartmentId = @departmentId ");
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    keyword = "%" + keyword + "%";
                    strSql.Append(" AND( t.F_Account like @keyword or t.F_RealName like @keyword  or t.F_Mobile like @keyword  ) ");
                }

                return this.BaseRepository().FindList<UserEntity>(strSql.ToString(), new { companyId, departmentId, keyword }, pagination);
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }

        /// <summary>
        /// 用户列表,全部
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UserEntity> GetAllList()
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("SELECT ");
                strSql.Append(fieldSql.Replace("t.F_Password,", "").Replace("t.F_Secretkey,", ""));
                strSql.Append(" FROM LR_Base_User t WHERE t.F_DeleteMark = 0  ORDER BY t.F_CompanyId,t.F_DepartmentId,t.F_RealName ");
                return this.BaseRepository().FindList<UserEntity>(strSql.ToString());
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        /// <summary>
        /// 用户列表（导出Excel）
        /// </summary>
        /// <returns></returns>
        public DataTable GetExportList()
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append(@"SELECT u.F_Account
                                  ,u.F_RealName
                                  ,CASE WHEN u.F_Gender=1 THEN '男' ELSE '女' END AS F_Gender
                                  ,u.F_Birthday
                                  ,u.F_Mobile
                                  ,u.F_Telephone
                                  ,u.F_Email
                                  ,u.F_WeChat
                                  ,u.F_MSN
                                  ,o.F_FullName AS F_Company
                                  ,d.F_FullName AS F_Department
                                  ,u.F_Description
                                  ,u.F_CreateDate
                                  ,u.F_CreateUserName
                              FROM LR_Base_User u
                              INNER JOIN LR_Base_Department d ON u.F_DepartmentId=d.F_DepartmentId
                              INNER JOIN LR_Base_Company o ON u.F_CompanyId=o.F_CompanyId WHERE u.F_DeleteMark = 0 ");
                return this.BaseRepository().FindTable(strSql.ToString());
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        /// <summary>
        /// 用户实体
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <returns></returns>
        public UserEntity GetEntity(string keyValue)
        {
            try
            {
                return this.BaseRepository().FindEntity<UserEntity>(t => t.F_UserId == keyValue && t.F_DeleteMark == 0);
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        /// <summary>
        /// 获取超级管理员用户列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UserEntity> GetAdminList()
        {
            try
            {
                return this.BaseRepository().FindList<UserEntity>(t => t.F_SecurityLevel == 1);
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }

        /// <summary>
        /// 获取用户数据
        /// </summary>
        /// <param name="userId">用户主键</param>
        /// <returns></returns>
        public UserEntity GetEntityByUserId(string userId)
        {
            try
            {
                UserEntity userEntity = cache.Read<UserEntity>(cacheKeyId + userId, CacheId.user);
                if (userEntity == null)
                {
                    userEntity = this.GetEntity(userId);
                    if (userEntity != null)
                    {
                        cache.Write<string>(cacheKeyAccount + userEntity.F_Account, userId, CacheId.user);
                        cache.Write<UserEntity>(cacheKeyId + userId, userEntity, CacheId.user);
                    }
                }
                return userEntity;
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowBusinessException(ex);
                }
            }
        }
        #endregion

        #region 验证数据
        /// <summary>
        /// 账户不能重复
        /// </summary>
        /// <param name="account">账户值</param>
        /// <param name="keyValue">主键</param>
        /// <returns></returns>
        public bool ExistAccount(string account, string keyValue)
        {
            try
            {
                var expression = LinqExtensions.True<UserEntity>();
                expression = expression.And(t => t.F_Account == account);
                if (!string.IsNullOrEmpty(keyValue))
                {
                    expression = expression.And(t => t.F_UserId != keyValue);
                }
                return this.BaseRepository().IQueryable(expression).Count() == 0 ? true : false;
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        #endregion

        #region 提交数据
        /// <summary>
        /// 虚拟删除
        /// </summary>
        /// <param name="keyValue">主键</param>
        public void VirtualDelete(string keyValue)
        {
            try
            {
                UserEntity entity = new UserEntity()
                {
                    F_UserId = keyValue,
                    F_DeleteMark = 1
                };
                this.BaseRepository().Update(entity);
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        /// <summary>
        /// 保存用户表单（新增、修改）
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="userEntity">用户实体</param>
        /// <returns></returns>
        public void SaveEntity(string keyValue, UserEntity userEntity)
        {
            try
            {
                if (string.IsNullOrEmpty(keyValue))
                {
                    userEntity.Create();
                    userEntity.F_Secretkey = Md5Helper.Encrypt(CommonHelper.CreateNo(), 16).ToLower();
                    userEntity.F_Password = Md5Helper.Encrypt(DESEncrypt.Encrypt(userEntity.F_Password, userEntity.F_Secretkey).ToLower(), 32).ToLower();
                    this.BaseRepository().Insert(userEntity);
                }
                else
                {
                    userEntity.Modify(keyValue);
                    userEntity.F_Secretkey = null;
                    userEntity.F_Password = null;
                    this.BaseRepository().Update(userEntity);
                }
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        /// <summary>
        /// 修改用户登录密码
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="password">新密码（MD5 小写）</param>
        public void RevisePassword(string keyValue, string password)
        {
            try
            {
                UserEntity userEntity = new UserEntity();
                userEntity.Modify(keyValue);
                userEntity.F_Secretkey = Md5Helper.Encrypt(CommonHelper.CreateNo(), 16).ToLower();
                userEntity.F_Password = Md5Helper.Encrypt(DESEncrypt.Encrypt(password, userEntity.F_Secretkey).ToLower(), 32).ToLower();
                this.BaseRepository().Update(userEntity);
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        /// <summary>
        /// 修改用户状态
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="state">状态：1-启动；0-禁用</param>
        public void UpdateState(string keyValue, int state)
        {
            try
            {
                UserEntity userEntity = new UserEntity();
                userEntity.Modify(keyValue);
                userEntity.F_EnabledMark = state;
                this.BaseRepository().Update(userEntity);
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        /// <summary>
        /// 修改用户信息
        /// </summary>
        /// <param name="userEntity">实体对象</param>
        public void UpdateEntity(UserEntity userEntity)
        {
            try
            {
                this.BaseRepository().Update(userEntity);
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowServiceException(ex);
                }
            }
        }
        #endregion


        #region 扩展方法
        /// <summary>
        /// 验证登录
        /// </summary>
        /// <param name="account">账号</param>
        /// <param name="password">密码 MD5 32位 小写</param>
        /// <returns></returns>
        public UserEntity CheckLogin(string account, string password)
        {
            ////调用微信开放平台接口获得Token、OpenId
            //string appid = Config.GetValue("AppId");
            //string appsecret = Config.GetValue("AppSecret");
            //OpenTokenGet openTokenGet = new OpenTokenGet();
            //openTokenGet.appid = appid;
            //openTokenGet.secret = appsecret;
            //openTokenGet.code = "0815LTNN0EEei42rURNN0z5QNN05LTNS";
            //OpenTokenGetResult openInfo = openTokenGet.OpenSend();
            //string openid = openInfo.openid;
            //string token = openInfo.access_token;
            ////调用微信开放平台接口获得登录用户个人信息
            //OpenUserGet openuser = new OpenUserGet();
            //openuser.openid = openid;
            //openuser.access_token = token;
            //OpenUserGetResult userinfo = openuser.OpenSend();
            try
            {
                UserEntity userEntity = GetEntityByAccount(account);
                if (userEntity == null)
                {
                    userEntity = new UserEntity()
                    {
                        LoginMsg = "账户不存在!",
                        LoginOk = false
                    };
                    return userEntity;
                }
                userEntity.LoginOk = false;
                if (userEntity.F_EnabledMark == 1)
                {
                    string dbPassword = Md5Helper.Encrypt(DESEncrypt.Encrypt(password.ToLower(), userEntity.F_Secretkey).ToLower(), 32).ToLower();
                    if (dbPassword == userEntity.F_Password)
                    {
                        userEntity.LoginOk = true;
                    }
                    else
                    {
                        userEntity.LoginMsg = "密码和账户名不匹配!";
                    }
                }
                else
                {
                    userEntity.LoginMsg = "账户被系统锁定,请联系管理员!";
                }
                return userEntity;
            }
            catch (Exception ex)
            {
                if (ex is ExceptionEx)
                {
                    throw;
                }
                else
                {
                    throw ExceptionEx.ThrowBusinessException(ex);
                }
            }
        }
        #endregion

    }
}
