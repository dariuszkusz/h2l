import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GroupsApiService } from './groups-api.service';

describe('GroupsApiService', () => {
  let service: GroupsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(GroupsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lists and creates groups', () => {
    service.listGroups().subscribe((groups) => {
      expect(groups).toHaveLength(1);
    });
    httpMock
      .expectOne('/api/v1/groups')
      .flush([
        { id: 'g1', name: 'HR', code: 'HR', memberCount: 2, createdAt: '2026-11-25T20:00:00Z' },
      ]);

    service.createGroup({ name: 'Sales', code: 'SALES' }).subscribe((group) => {
      expect(group.code).toBe('SALES');
    });
    const req = httpMock.expectOne('/api/v1/groups');
    expect(req.request.method).toBe('POST');
    req.flush({
      id: 'g2',
      name: 'Sales',
      code: 'SALES',
      memberCount: 0,
      createdAt: '2026-11-25T20:00:00Z',
    });
  });

  it('lists, adds, and removes group members', () => {
    service.listGroupMembers('g1').subscribe((members) => {
      expect(members).toHaveLength(0);
    });
    httpMock.expectOne('/api/v1/groups/g1/members').flush([]);

    service.addGroupMember('g1', { userId: 'u1' }).subscribe((member) => {
      expect(member.userId).toBe('u1');
    });
    const addReq = httpMock.expectOne('/api/v1/groups/g1/members');
    expect(addReq.request.method).toBe('POST');
    addReq.flush({
      userId: 'u1',
      email: 'u1@example.com',
      systemRole: 'EndUser',
      isActive: true,
    });

    service.removeGroupMember('g1', 'u1').subscribe();
    const delReq = httpMock.expectOne('/api/v1/groups/g1/members/u1');
    expect(delReq.request.method).toBe('DELETE');
    delReq.flush(null);
  });
});
