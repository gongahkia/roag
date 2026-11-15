import 'rules.dart';

enum Need { work, workAccess, social }

enum RequestState { accepted, resolved, cancelled }

extension NeedText on Need {
  String get label => switch (this) {
    Need.work => 'Reach suitable work',
    Need.workAccess => 'Reconnect work',
    Need.social => 'Reach a social venue',
  };
  String get appeal => switch (this) {
    Need.work => 'I have been looking for work. Could you make room at a reachable workplace?',
    Need.workAccess =>
      'I cannot reach my assigned workplace. Could you reconnect the path?',
    Need.social => 'I have not been able to reach a social venue. A connected Park, Hall or staffed Market would help.',
  };
  String get thanks => this == Need.social
      ? 'I finally had a proper visit. Thank you for making space for us.'
      : 'I have actually been back at work. Thank you for helping.';
}

class ResidentRequest {
  final String id, resident;
  final Need need;
  final double acceptedAt;
  RequestState state;
  double? closedAt;
  double rewardUntil;
  ResidentRequest(
    this.id,
    this.resident,
    this.need,
    this.acceptedAt, {
    this.state = RequestState.accepted,
    this.closedAt,
    this.rewardUntil = -1,
  });
  String get status => switch (state) {
    RequestState.accepted => '${need.label} · promised',
    RequestState.resolved => '${need.label} · helped',
    RequestState.cancelled => '${need.label} · withdrawn',
  };
  Map<String, Object?> toJson() => {
    'id': id,
    'resident': resident,
    'need': need.name,
    'acceptedAt': acceptedAt,
    'state': state.name,
    'closedAt': closedAt,
    'rewardUntil': rewardUntil,
  };
}

// only open promises are ticked; the per-concern index keeps rewards one-shot.
class RequestBook {
  int nextId = 1;
  final Map<String, ResidentRequest> records = {}, open = {};
  final Map<String, ResidentRequest> _concerns = {};
  ResidentRequest? concern(String resident, Need need) =>
      _concerns['$resident/${need.name}'];
  Iterable<ResidentRequest> forResident(String id) sync* {
    for (final need in Need.values) {
      final request = concern(id, need);
      if (request != null) yield request;
    }
  }

  String? unavailable(String id, Need need) => concern(id, need) != null
      ? 'This concern has already been discussed.'
      : open.length >= Rules.requestLimit
      ? 'Three promises are already open. Help someone or withdraw a promise first.'
      : null;
  ResidentRequest? accept(String resident, Need need, double clock) {
    if (unavailable(resident, need) != null) return null;
    final r = ResidentRequest('request-${nextId++}', resident, need, clock);
    records[r.id] = r;
    open[r.id] = r;
    _concerns['$resident/${need.name}'] = r;
    return r;
  }

  bool close(String id, double clock, {required bool resolved}) {
    final r = open.remove(id);
    if (r == null) return false;
    r.state = resolved ? RequestState.resolved : RequestState.cancelled;
    r.closedAt = clock;
    r.rewardUntil = resolved ? clock + Rules.requestDuration : -1;
    return true;
  }

  int mood(String resident, double clock) {
    if (records.isEmpty) return 0;
    return forResident(resident).any((r) => r.rewardUntil > clock)
        ? Rules.requestBenefit
        : 0;
  }

  Map<String, Object> toJson() => {
    'nextId': nextId,
    'records': records.values.map((r) => r.toJson()).toList(),
  };
  factory RequestBook.decode(
    dynamic data,
    Set<String> residents,
    Set<String> active,
    double clock,
  ) {
    final book = RequestBook();
    if (data == null) return book;
    if (data is! Map ||
        data['nextId'] is! int ||
        data['nextId'] < 1 ||
        data['nextId'] > 1000000000 ||
        data['records'] is! List ||
        data['records'].length > residents.length * Need.values.length) {
      throw const FormatException('Invalid resident requests.');
    }
    book.nextId = data['nextId'];
    double time(dynamic value) {
      if (value is! num || !value.isFinite || value < 0 || value > clock) {
        throw const FormatException('Invalid request time.');
      }
      return value.toDouble();
    }

    for (final row in data['records']) {
      if (row is! Map ||
          row['id'] is! String ||
          !RegExp(r'^request-[1-9][0-9]*$').hasMatch(row['id']) ||
          int.parse(row['id'].split('-').last) >= book.nextId ||
          !residents.contains(row['resident'])) {
        throw const FormatException('Invalid request identity.');
      }
      final need = Need.values.firstWhere(
        (n) => n.name == row['need'],
        orElse: () => throw const FormatException('Invalid request need.'),
      );
      final state = RequestState.values.firstWhere(
        (s) => s.name == row['state'],
        orElse: () => throw const FormatException('Invalid request status.'),
      );
      final r = ResidentRequest(
        row['id'],
        row['resident'],
        need,
        time(row['acceptedAt']),
        state: state,
      );
      r.closedAt = row['closedAt'] == null ? null : time(row['closedAt']);
      final reward = row['rewardUntil'];
      if (reward is! num ||
          !reward.isFinite ||
          (state == RequestState.accepted &&
              (r.closedAt != null || !active.contains(r.resident))) ||
          (state != RequestState.accepted &&
              (r.closedAt == null || r.closedAt! < r.acceptedAt)) ||
          (state == RequestState.resolved
              ? (reward - r.closedAt! - Rules.requestDuration).abs() > 1e-6
              : reward != -1) ||
          book.records.containsKey(r.id) ||
          book.concern(r.resident, need) != null) {
        throw const FormatException('Invalid request consequence.');
      }
      r.rewardUntil = reward.toDouble();
      book.records[r.id] = r;
      book._concerns['${r.resident}/${need.name}'] = r;
      if (state == RequestState.accepted) book.open[r.id] = r;
    }
    if (book.open.length > Rules.requestLimit) {
      throw const FormatException('Too many open requests.');
    }
    return book;
  }
  RequestBook();
}
