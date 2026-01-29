{{- define "ats-frontend.name" -}}
ats-frontend
{{- end }}

{{- define "ats-frontend.fullname" -}}
{{ printf "%s" (include "ats-frontend.name" .) }}
{{- end }}
