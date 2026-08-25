using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data.Configurations;

public class SystemConstantConfiguration : IEntityTypeConfiguration<SystemConstant>
{
    public void Configure(EntityTypeBuilder<SystemConstant> builder)
    {
        builder.ToTable("SystemConstants");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Category).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(200);
        builder.HasIndex(s => new { s.Category, s.Value }).IsUnique();

        // بيانات البذر الأولية
        int id = 1;
        var seed = new List<SystemConstant>();

        // التحصيل الدراسي
        foreach (var v in new[] { "أمي","يقرأ ويكتب","ابتدائية","متوسطة","إعدادية","دبلوم","بكالوريوس","دبلوم عالي","ماجستير","دكتوراه" })
            seed.Add(new SystemConstant { Id = id++, Category = SysConst.EducationLevel, Value = v, DisplayOrder = id });

        // مجال الاستفادة
        foreach (var v in new[] { "تنظيمي وإداري","إعلامي","قانوني","مالي ومحاسبي","طبي وصحي","هندسي وفني","تربوي وتعليمي","تقنية المعلومات","علاقات عامة","لوجستي وخدمي","تحريري وجماهيري","بحوث ودراسات","أخرى" })
            seed.Add(new SystemConstant { Id = id++, Category = SysConst.BenefitField, Value = v, DisplayOrder = id });

        // صفة المسؤول
        foreach (var v in new[] { "رئيس","نائب رئيس","أمين سر","عضو مجلس","موظف","منسق","متطوع" })
            seed.Add(new SystemConstant { Id = id++, Category = SysConst.ManagerTitle, Value = v, DisplayOrder = id });

        builder.HasData(seed);
    }
}
