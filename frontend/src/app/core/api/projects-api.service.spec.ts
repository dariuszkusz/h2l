import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProjectsApiService } from './projects-api.service';

describe('ProjectsApiService', () => {
  let service: ProjectsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProjectsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lists and creates projects and processes', () => {
    service.listProjects().subscribe((projects) => {
      expect(projects).toHaveLength(1);
    });
    httpMock
      .expectOne('/api/v1/projects')
      .flush([{ id: 'p1', name: 'HR', description: null, createdAt: '2026-11-25T20:00:00Z' }]);

    service.createProject({ name: 'Sales', description: 'Deals' }).subscribe((project) => {
      expect(project.name).toBe('Sales');
    });
    const createProjReq = httpMock.expectOne('/api/v1/projects');
    expect(createProjReq.request.method).toBe('POST');
    createProjReq.flush({
      id: 'p2',
      name: 'Sales',
      description: 'Deals',
      createdAt: '2026-11-25T20:00:00Z',
    });

    service
      .createWorkflowProcess('p1', {
        name: 'Purchase',
        externalProcessKey: 'purchase',
        status: 'Active',
      })
      .subscribe((proc) => {
        expect(proc.externalProcessKey).toBe('purchase');
      });
    const createProcReq = httpMock.expectOne('/api/v1/projects/p1/processes');
    expect(createProcReq.request.method).toBe('POST');
    createProcReq.flush({
      id: 'proc1',
      projectId: 'p1',
      name: 'Purchase',
      externalProcessKey: 'purchase',
      status: 'Active',
      createdAt: '2026-11-25T20:00:00Z',
    });
  });

  it('manages process forms and ACL rules', () => {
    service
      .createFormDefinition('proc1', { name: 'Request', stepKey: 'step-1', draftSchemaJsonb: {} })
      .subscribe((form) => {
        expect(form.stepKey).toBe('step-1');
      });
    const formReq = httpMock.expectOne('/api/v1/processes/proc1/forms');
    expect(formReq.request.method).toBe('POST');
    formReq.flush({
      id: 'f1',
      workflowProcessId: 'proc1',
      name: 'Request',
      stepKey: 'step-1',
      status: 'Draft',
      draftSchemaJsonb: {},
      updatedAt: '2026-11-25T20:00:00Z',
    });

    service.getFormAccessRules('f1').subscribe((acl) => {
      expect(acl.formId).toBe('f1');
    });
    httpMock.expectOne('/api/v1/forms/f1/access-rules').flush({ formId: 'f1', rules: [] });

    service
      .updateFormAccessRules('f1', { rules: [{ groupId: 'g1', userId: null }] })
      .subscribe();
    const putReq = httpMock.expectOne('/api/v1/forms/f1/access-rules');
    expect(putReq.request.method).toBe('PUT');
    putReq.flush(null);
  });
});
