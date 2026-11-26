using System.Text.Json;
using H2l.Domain.Common;

namespace H2l.Application.Forms.Schema;

public interface IFormSchemaValidator
{
    Result<FormSchemaDocument> ParseAndValidate(string schemaJson);

    Result<FormSchemaDocument> ParseAndValidate(JsonElement schemaElement);

    Result Validate(FormSchemaDocument schema);
}
