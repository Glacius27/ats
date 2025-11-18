{{- define "ats-candidate-service.name" -}}
ats-candidate-service
{{- end }}

{{- define "ats-candidate-service.fullname" -}}
{{ printf "%s" (include "ats-candidate-service.name" .) }}
{{- end }}