using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.GetSchoolDay;

/// <summary>Bugün (Türkiye günü) verilen dönemde okul günü mü (spec 0010): yoklama bu sorguyla "okul günü değil"i belirler.</summary>
public sealed record GetSchoolDayQuery(Guid TermId) : IRequest<SchoolDay>;
