{{- define "ats-interview-service.name" -}}
ats-interview-service
{{- end }}

{{- define "ats-interview-service.fullname" -}}
{{ printf "%s" (include "ats-interview-service.name" .) }}
{{- end }}
