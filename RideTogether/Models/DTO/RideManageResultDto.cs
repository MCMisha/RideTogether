using RideTogether.Entities;
using RideTogether.Entities.Enums;
using RideTogether.Models.Enums;

namespace RideTogether.Models.DTO;

public record RideManageResultDto(RideManageResult Result, RideResponseDto? Value = null);