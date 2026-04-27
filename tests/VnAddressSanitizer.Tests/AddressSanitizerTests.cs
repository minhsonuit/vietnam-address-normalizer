using FluentAssertions;
using Xunit;

namespace VnAddressSanitizer.Tests;

/// <summary>1. Normalization tests</summary>
public class NormalizationTests
{
    [Fact] public void Null_returns_empty() => AddressSanitizer.Sanitize(null).Should().BeEmpty();
    [Fact] public void Empty_returns_empty() => AddressSanitizer.Sanitize("").Should().BeEmpty();
    [Fact] public void Whitespace_returns_empty() => AddressSanitizer.Sanitize("   ").Should().BeEmpty();
    [Fact] public void Collapses_multiple_spaces() => AddressSanitizer.Sanitize("31   Trương   Phước   Phan").Should().Be("31 Trương Phước Phan");
}

/// <summary>1.5 Abbreviation expansion tests</summary>
public class AbbreviationExpansionTests
{
    [Theory]
    [InlineData("123 Lê Lợi, Q.1, TP. HCM", "123 Lê Lợi, Quận 1, Thành phố HCM")]
    [InlineData("456, p12, q 10", "456, Phường 12, Quận 10")]
    [InlineData("789, TX. Tân Uyên, Tỉnh Bình Dương", "789, Thị xã Tân Uyên, Tỉnh Bình Dương")]
    [InlineData("Khu phố 1, TT. Trảng Bom", "Khu phố 1, Thị trấn Trảng Bom")]
    public void Expands_common_abbreviations(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);

    [Fact]
    public void Does_not_expand_when_disabled()
    {
        var opts = new SanitizeOptions { ExpandAbbreviations = false };
        AddressSanitizer.Sanitize("123 Lê Lợi, Q.1, TP. HCM", opts).Should().Be("123 Lê Lợi, Q.1, TP. HCM");
    }
}


/// <summary>2. Parentheses removal tests</summary>
public class ParenthesesTests
{
    [Fact]
    public void Removes_parenthetical_content()
        => AddressSanitizer.Sanitize("1279 Nguyễn Tất Thành (Khách Sạn Hiền Thuận)")
            .Should().Be("1279 Nguyễn Tất Thành");

    [Fact]
    public void Removes_near_market_note()
        => AddressSanitizer.Sanitize("23 Lê Văn Việt (gần chợ)")
            .Should().Be("23 Lê Văn Việt");

    [Fact]
    public void Keeps_parens_when_disabled()
    {
        var opts = new SanitizeOptions { RemoveParentheses = false };
        AddressSanitizer.Sanitize("23 Lê Văn Việt (gần chợ)", opts)
            .Should().Contain("(gần chợ)");
    }
}

/// <summary>3. Phone removal tests</summary>
public class PhoneRemovalTests
{
    [Theory]
    [InlineData("0868047361 31 trương phước phan", "31 trương phước phan")]
    [InlineData("0356 493 913, Quận 1", "Quận 1")]
    [InlineData("+84 356 493 913, Quận 1", "Quận 1")]
    public void Removes_phone_numbers(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);

    [Theory]
    [InlineData("31 Trương Phước Phan")]
    [InlineData("031 Nguyễn Văn Cừ")]
    [InlineData("101/5 Lê Lợi")]
    public void Does_not_remove_house_numbers(string input)
        => AddressSanitizer.Sanitize(input).Should().Be(input);
}

/// <summary>4. Contact label cleanup tests</summary>
public class ContactLabelTests
{
    [Fact]
    public void Removes_phone_label_after_number_removed()
        => AddressSanitizer.Sanitize("phone: 0944701399, 28b hai bà trưng")
            .Should().Be("28b hai bà trưng");

    [Fact]
    public void Removes_sdt_label()
        => AddressSanitizer.Sanitize("dt 0912345678, TP Thủ Đức")
            .Should().Be("Thành phố Thủ Đức");
}

/// <summary>5. Instruction removal tests</summary>
public class InstructionRemovalTests
{
    [Fact]
    public void Removes_call_and_delivery_instruction()
        => AddressSanitizer.Sanitize("0868047361 gọi này nhận hàng dùm em, 31 trương phước phan")
            .Should().Be("31 trương phước phan");

    [Fact]
    public void Removes_dont_call_instruction()
        => AddressSanitizer.Sanitize("Đừng gọi số kia gọi giúp em số này 033 467 8793, 10 Nguyễn Huệ, Quận 1")
            .Should().Be("10 Nguyễn Huệ, Quận 1");

    [Fact]
    public void Does_not_remove_standalone_giao()
        => AddressSanitizer.Sanitize("Đường Thuận Giao 25")
            .Should().Be("Đường Thuận Giao 25");

    [Theory]
    [InlineData("146 bis Hồ Tùng Mậu - giao cổng bảo vệ, Phường Võ Cường", "146 bis Hồ Tùng Mậu, Phường Võ Cường")]
    [InlineData("819 Cách Mạng Tháng 8 - giao cổng bảo vệ, Phường Đại Phúc", "819 Cách Mạng Tháng 8, Phường Đại Phúc")]
    public void Removes_giao_cong_bao_ve(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);

    [Theory]
    [InlineData("65A Lạc Long Quân - call truoc khi den, Phường Khương Trung", "65A Lạc Long Quân, Phường Khương Trung")]
    [InlineData("94 bis Pasteur - call truoc khi den, Xã Vĩnh Ngọc", "94 bis Pasteur, Xã Vĩnh Ngọc")]
    public void Removes_call_truoc_khi_den(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);

    [Theory]
    [InlineData("123 Lê Lợi, để trước cổng, Quận 1", "123 Lê Lợi, Quận 1")]
    [InlineData("456 Nguyễn Trãi - bỏ trước cửa giùm, Phường 8", "456 Nguyễn Trãi, Phường 8")]
    [InlineData("789 Trần Hưng Đạo, gửi bảo vệ tòa nhà, Quận 5", "789 Trần Hưng Đạo, Quận 5")]
    [InlineData("12 Điện Biên Phủ, để ở hòm thư, Phường 10", "12 Điện Biên Phủ, Phường 10")]
    [InlineData("34 Hai Bà Trưng - bỏ ở ngoài, Quận 3", "34 Hai Bà Trưng, Quận 3")]
    [InlineData("56 Lý Thường Kiệt - giao trong giờ hành chính, Quận 10", "56 Lý Thường Kiệt, Quận 10")]
    [InlineData("78 Cách Mạng Tháng 8 - nhận hộ, Tân Bình", "78 Cách Mạng Tháng 8, Tân Bình")]
    public void Removes_placement_and_reception_instructions(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);

    [Theory]
    [InlineData("đ/c: 123 Lê Lợi, Quận 1", "123 Lê Lợi, Quận 1")]
    [InlineData("Địa chỉ: 456 Nguyễn Trãi, Phường 8", "456 Nguyễn Trãi, Phường 8")]
    [InlineData("Nơi giao: 789 Trần Hưng Đạo, Quận 5", "789 Trần Hưng Đạo, Quận 5")]
    public void Removes_address_labels(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);

    [Theory]
    [InlineData("123 Lê Lợi,mã đơn: ABC12345, Quận 1", "123 Lê Lợi, Quận 1")]
    [InlineData("456 Nguyễn Trãi, đơn hàng 999888, Phường 8", "456 Nguyễn Trãi, Phường 8")]
    [InlineData("789 Trần Hưng Đạo, cảm ơn shop, Quận 5", "789 Trần Hưng Đạo, Quận 5")]
    [InlineData("12 Điện Biên Phủ, thanks, Phường 10", "12 Điện Biên Phủ, Phường 10")]
    public void Removes_order_and_thank_you_notes(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);
}

/// <summary>6. Direction note removal tests</summary>
public class DirectionNoteTests
{
    [Theory]
    [InlineData("873 Lang - cuối hẻm - Gò Vấp - Da Nang", "873 Lang - Gò Vấp - Da Nang")]
    [InlineData("158B Giai Phong, gần coopmart, Hải Phòng", "158B Giai Phong, Hải Phòng")]
    [InlineData("37B Bạch Đằng - mặt tiền - Quận 1 - Đà Nẵng", "37B Bạch Đằng - Quận 1 - Đà Nẵng")]
    [InlineData("193B Lý Thường Kiệt - next to Vinmart, Phường Phước Long", "193B Lý Thường Kiệt, Phường Phước Long")]
    [InlineData("275/56 bach dang - next to vinmart, Phường Lộc Thọ", "275/56 bach dang, Phường Lộc Thọ")]
    [InlineData("123 Main St - across from park, Quận 1", "123 Main St, Quận 1")]
    [InlineData("456 High St, in front of school, Quận 2", "456 High St, Quận 2")]
    [InlineData("789 Broad St - beside hospital, Quận 3", "789 Broad St, Quận 3")]
    [InlineData("gần chợ Bến Thành, 123 Lê Lợi", "123 Lê Lợi")]
    [InlineData("đối diện trường học, 456 Nguyễn Trãi", "456 Nguyễn Trãi")]
    [InlineData("phía sau nhà thờ, 789 Trần Hưng Đạo", "789 Trần Hưng Đạo")]
    public void Removes_direction_notes(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);
}

/// <summary>7. Postal/country cleanup tests</summary>
public class PostalCountryTests
{
    [Theory]
    [InlineData("Hồ Chí Minh 700000, Việt Nam", "Hồ Chí Minh")]
    [InlineData("Hue 700000", "Hue")]
    [InlineData("Long An 100000 #", "Long An")]
    public void Removes_postal_and_country(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);
}

/// <summary>8. Standalone junk cleanup tests</summary>
public class StandaloneJunkTests
{
    [Fact]
    public void Removes_hash_between_separators()
        => AddressSanitizer.Sanitize("189A Đường số 5, #, Xã Vĩnh Ngọc")
            .Should().Be("189A Đường số 5, Xã Vĩnh Ngọc");

    [Theory]
    [InlineData("319 Nguyễn Trãi ???, Xã Cam Hải Đông", "319 Nguyễn Trãi, Xã Cam Hải Đông")]
    [InlineData("521 Đường D1, ???, Xã Phước Lý", "521 Đường D1, Xã Phước Lý")]
    [InlineData("59B Cach Mang Thang 8 ???, Phường Võ Cường", "59B Cach Mang Thang 8, Phường Võ Cường")]
    public void Removes_question_mark_junk(string input, string expected)
        => AddressSanitizer.Sanitize(input).Should().Be(expected);
}

/// <summary>9. Admin dedup tests</summary>
public class AdminDedupTests
{
    [Fact]
    public void Deduplicates_exact_suffix()
    {
        var input = "23c2 đường 11 phường linh xuân quận thủ đức, Phường Linh Xuân, Thành phố Thủ Đức, Thành phố Hồ Chí Minh, Phường Linh Xuân, Thành phố Thủ Đức, Thành phố Hồ Chí Minh";
        var expected = "23c2 đường 11 phường linh xuân quận thủ đức, Phường Linh Xuân, Thành phố Thủ Đức, Thành phố Hồ Chí Minh";
        AddressSanitizer.Sanitize(input).Should().Be(expected);
    }

    [Fact]
    public void Deduplicates_informal_to_formal()
    {
        var input = "28 Đường số 3, Bình Hưng, Bình Chánh, TP HCM, Xã Bình Hưng, Huyện Bình Chánh, Thành phố Hồ Chí Minh";
        var expected = "28 Đường số 3, Xã Bình Hưng, Huyện Bình Chánh, Thành phố Hồ Chí Minh";
        AddressSanitizer.Sanitize(input).Should().Be(expected);
    }
}

/// <summary>10. False positive regression tests</summary>
public class FalsePositiveRegressionTests
{
    [Theory]
    [InlineData("Đường Thuận Giao 25")]
    [InlineData("31 Trương Phước Phan")]
    [InlineData("031 Nguyễn Văn Cừ")]
    [InlineData("101/5 Lê Lợi")]
    [InlineData("Chợ Bến Thành, Quận 1")]
    [InlineData("123 Đường Nam, Xã Giao Khẩu")]
    [InlineData("456 Callisto Tower, Phường 1")]
    [InlineData("789 Near East Plaza, Quận 2")]
    [InlineData("Block P12, Quận 7")]
    [InlineData("Tower Q1, Phường Bến Nghé")]
    [InlineData("Khu P.12, Quận 7")]
    [InlineData("123 Đường Gửi Bảo Vệ, Quận 1")]
    [InlineData("456 Hẻm Nhận Hộ, Phường 5")]
    [InlineData("789 Đường Để Trước, Phường 10")]
    [InlineData("12 Đường Bỏ Ngoài, Phường 4")]
    [InlineData("Đường Giao Tới, Quận 1")]
    [InlineData("Đường Thank, Phường Thanks, Quận 1")]
    [InlineData("123 Road, Near East Plaza, Quận 2")]
    [InlineData("456 Road, Opposite House, Quận 7")]
    [InlineData("789 Road, Behind Tower, Phường 1")]
    [InlineData("101 Road, Beside Garden, Phường Thảo Điền")]
    public void Must_not_alter_valid_addresses(string input)
        => AddressSanitizer.Sanitize(input).Should().Be(input);
}

/// <summary>11. Option behavior tests</summary>
public class OptionBehaviorTests
{
    [Fact]
    public void BuildingInfo_disabled_by_default()
    {
        AddressSanitizer.Sanitize("Eco Green, phòng B951, Quận 7")
            .Should().Contain("phòng B951");
    }

    [Fact]
    public void BuildingInfo_enabled_removes_info()
    {
        var opts = new SanitizeOptions { RemoveBuildingInfo = true };
        AddressSanitizer.Sanitize("Eco Green, phòng B951, Quận 7", opts)
            .Should().NotContain("phòng B951");
    }

    [Fact]
    public void ExtendedBuildingInfo_enabled_removes_kdt_chungcu()
    {
        var opts = new SanitizeOptions { RemoveBuildingInfo = true, ExpandAbbreviations = false };
        AddressSanitizer.Sanitize("Chung cư Vinhome, KĐT Sala, Quận 2", opts)
            .Should().Be("Quận 2");
    }

    [Fact]
    public void Custom_patterns_applied()
    {
        var opts = new SanitizeOptions { AdditionalPatterns = new[] { @"\bCustomNoise\b" } };
        AddressSanitizer.Sanitize("123 Lê Lợi CustomNoise, Quận 1", opts)
            .Should().Be("123 Lê Lợi, Quận 1");
    }
}

/// <summary>Integration tests matching the prompt specification</summary>
public class IntegrationTests
{
    [Fact]
    public void Full_pipeline_phone_and_instruction()
        => AddressSanitizer.Sanitize("0868047361 gọi này nhận hàng dùm em, 31 trương phước phan")
            .Should().Be("31 trương phước phan");

    [Fact]
    public void Full_pipeline_phone_label_with_admin()
        => AddressSanitizer.Sanitize("phone: 0944701399, 28b hai bà trưng, Xã Bình Hưng")
            .Should().Be("28b hai bà trưng, Xã Bình Hưng");

    [Fact]
    public void Full_pipeline_complex_tel_with_junk()
        => AddressSanitizer.Sanitize("189A Đường số 5, tel: 0955149984 #, Xã Vĩnh Ngọc")
            .Should().Be("189A Đường số 5, Xã Vĩnh Ngọc");

    [Fact]
    public void Full_pipeline_postal_and_country()
        => AddressSanitizer.Sanitize("Hồ Chí Minh 700000, Việt Nam")
            .Should().Be("Hồ Chí Minh");

    [Fact]
    public void Full_pipeline_call_instruction_removes_dash_artifact()
        => AddressSanitizer.Sanitize("65A Lạc Long Quân - call truoc khi den, Phường Khương Trung, Thành phố Hà Nội")
            .Should().Be("65A Lạc Long Quân, Phường Khương Trung, Thành phố Hà Nội");

    [Fact]
    public void Full_pipeline_next_to_english_landmark()
        => AddressSanitizer.Sanitize("243 Pham Van Dong - next to Vinmart, Phường Khuê Mỹ, Quận Ngũ Hành Sơn, Thành phố Đà Nẵng")
            .Should().Be("243 Pham Van Dong, Phường Khuê Mỹ, Quận Ngũ Hành Sơn, Thành phố Đà Nẵng");

    [Fact]
    public void Full_pipeline_giao_cong_bao_ve()
        => AddressSanitizer.Sanitize("485 Trần Duy Hưng - giao cổng bảo vệ, Xã Mỹ Khánh, Thành phố Cần Thơ")
            .Should().Be("485 Trần Duy Hưng, Xã Mỹ Khánh, Thành phố Cần Thơ");

    [Fact]
    public void Full_pipeline_question_marks_after_phone()
        => AddressSanitizer.Sanitize("521 Đường D1, phone: 0950061537 ???, Xã Phước Lý, Thành phố Tân An, Tỉnh Long An")
            .Should().Be("521 Đường D1, Xã Phước Lý, Thành phố Tân An, Tỉnh Long An");
}
