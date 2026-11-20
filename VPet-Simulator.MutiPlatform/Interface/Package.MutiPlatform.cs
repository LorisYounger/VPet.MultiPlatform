//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/Package.Windows.cs, 逐字相同
using System;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Windows.Interface
{
    public partial class ScheduleTask
    {
        /// <summary>
        /// 根据本平台的 PackageFull 创建已签约的套餐。
        /// </summary>
        public partial class Package
        {
            public Package(PackageFull packageFull, int level)
            {
                Name = packageFull.Name;
                Describe = packageFull.Describe;
                Commissions = packageFull.Commissions;
                Price = SchedulePackageRules.SignPrice(packageFull.Price, level);
                EndTime = SchedulePackageRules.EndTime(DateTime.Now, packageFull.Duration);
                Level = SchedulePackageRules.GrantedLevel(level, packageFull.LevelInNeed);
            }
        }
    }
}
